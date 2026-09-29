using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.JavaScript
{
    /// <summary>
    /// Hierarchical assembler. It receives independent token nodes and packs
    /// them through the language semantic assembler.
    /// </summary>
    public class Assembler : MiMFa.Engine.Assembler.Assembler
    {
        public int Location { get; set; } = 0;
        public Node LastNode { get; set; } = null;
        public NodeWalker Assembled { get; set; } = null;

        public override IEnumerable<Node> Assemble(NodeWalker walker, MiMFa.Engine.Engine compiler = null)
        {
            if (!Initialize(compiler)) return new Node[0];
            Assembled = new NodeWalker();
            while (!walker.IsEnded)
            {
                Location = 0;
                Assembled.Add(SectionAssembleNode(walker));
            }
            return Assembled.Content;
        }

        protected virtual Node SectionAssembleNode(NodeWalker walker)
        {
            Location++;
            var nodes = SectionAssembleNodes(walker).ToList();
            Location--;
            if (nodes.Count > 1) return new Node(null, NodeType.Chunk, Location + 1, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location: Location + 1);
            return new Node(location: Location + 1);
        }
        protected virtual IEnumerable<Node> SectionAssembleNodes(NodeWalker walker)
        {
            foreach (var node in SequenceAssembleNodes(walker)) yield return node;
            var next = walker.PeekProcedure();
            if (next != null && !(Compiler as Engine).IsFlag(next))
                if (next.Is(TokenType.Symbol))
                    foreach (var node in SectionAssembleNodes(walker))
                        yield return node;
        }

        protected virtual Node SequenceAssembleNode(NodeWalker walker)
        {
            Location++;
            var nodes = SequenceAssembleNodes(walker).ToList();
            Location--;
            if (nodes.Count > 1) return new Node(null, NodeType.Chunk, Location + 1, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location: Location + 1);
            return new Node(location: Location + 1);
        }
        protected virtual IEnumerable<Node> SequenceAssembleNodes(NodeWalker walker)
        {
            foreach (var node in CompactAssembleNodes(walker)) yield return node;
            var next = walker.PeekProcedure();
            if (next != null && !(Compiler as Engine).IsFlag(next))
                if (next.Is(TokenType.DelimiterSymbol))
                    foreach (var node in SequenceAssembleNodes(walker))
                        yield return node;
        }

        protected virtual Node CompactAssembleNode(NodeWalker walker)
        {
            var nodes = CompactAssembleNodes(walker).ToList();
            if (nodes.Count > 1) return new Node(null, NodeType.Chunk, Location, nodes.ToArray());
            if (nodes.Count == 1) return nodes[0].Update(location: Location);
            return new Node(location: Location);
        }
        protected virtual IEnumerable<Node> CompactAssembleNodes(NodeWalker walker)
        {
            yield return AssembleNode(walker);
            var next = walker.PeekProcedure();
            if (next != null && !(Compiler as Engine).IsFlag(next))
                if (
                    next.Is(
                        TokenType.ConcatenatorSymbol,
                        TokenType.Suffix,
                        TokenType.Middle,
                        TokenType.End,
                        TokenType.Start | TokenType.Scope
                    )
                )
                    foreach (var node in CompactAssembleNodes(walker))
                        yield return node;
        }

        protected override Node AssembleNode(NodeWalker walker)
        {
            if ((Compiler as Engine).IsFlag(walker.Current)) return new Node();
            return base.AssembleNode(walker.Walk(), walker).Update(location: Location);
        }

        protected override Node AssembleStatementNode(Node node, NodeWalker walker)
        {
            switch (node.Token.Value)
            {
                case "get":
                case "set":
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));
                case "function":
                    var fname = walker.PeekProcedure();
                    if (fname != null && fname.Is(TokenType.Keyword))
                    {
                        (Compiler as Engine).SetKeyword(fname.Token.Update(TokenType.FunctionKeyword));
                        walker.Replace(fname);
                    }
                    //else node.Update(type: (NodeType)(node.Type - NodeType.Region) | NodeType.Line);
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));

                case "if":
                    return node.AddRange(AssembleNode(walker), SectionAssembleNode(walker), (walker.PeekProcedure()?.Token?.IsMatch("else") == true) ? SectionAssembleNode(walker) : null);
                case "else":
                    return node.AddRange(SectionAssembleNode(walker));

                case "switch":
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));
                case "case":
                case "default":
                    if (node.Token.Value == "case") node.AddRange(AssembleNode(walker), walker.WalkProcedure().Update(NodeType.Chunk));
                    else node.AddRange(walker.WalkProcedure().Update(NodeType.Chunk));
                    var items = walker.MapUntil(() =>
                            walker.Current == null ||
                            walker.Current.IsMatch("case", "default") ||
                            walker.Current.Is(TokenType.End | TokenType.Scope),
                        () => SectionAssembleNode(walker)).ToArray();
                    if (items.Length > 0) return node.Add(new Node(null, NodeType.BlockStructure, items));
                    else return node.Update((NodeType)(node.Type - NodeType.Line));

                case "for":
                    return node.AddRange(AssembleNode(walker), SectionAssembleNode(walker));

                case "while":
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));

                case "do":
                    return node.AddRange(AssembleNode(walker), SectionAssembleNode(walker));

                case "break":
                    return node.Add(AssembleNode(walker));
                case "continue":
                    return node.Add(AssembleNode(walker));

                case "try":
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));
                case "catch":
                    return node.AddRange(
                        walker.PeekProcedure()?.IsMatch("(") == true ? AssembleNode(walker) : null,
                        AssembleNode(walker),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? AssembleNode(walker) : null
                    );
                case "finally":
                    return node.AddRange(
                        AssembleNode(walker),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? AssembleNode(walker) : null
                        );

                case "return":
                case "yield":
                case "throw":
                    return node.Add(SectionAssembleNode(walker));

                case "import":
                case "export":
                    return node.Add(SectionAssembleNode(walker));

                case "void":
                    return node.Add(AssembleNode(walker));

                case "var":
                case "let":
                case "const":
                    return node.Add(SectionAssembleNode(walker));

                case "with":
                case "debugger":
                    return node.Add(SectionAssembleNode(walker));

                case "delete":
                    return node.Add(SectionAssembleNode(walker));

                case "await":
                case "async":
                case "new":
                    return node.Add(CompactAssembleNode(walker));

                case "private":
                    var n1 = SectionAssembleNode(walker);
                    if (n1 == null) return node;
                    n1.AccessType |= AccessType.Private;
                    return node.Add(n1);
                case "protected":
                    var n2 = SectionAssembleNode(walker);
                    if (n2 == null) return node;
                    n2.AccessType |= AccessType.Protected;
                    return node.Add(n2);
                case "internal":
                    var n3 = SectionAssembleNode(walker);
                    if (n3 == null) return node;
                    n3.AccessType |= AccessType.Internal;
                    return node.Add(n3);
                case "public":
                    var n4 = SectionAssembleNode(walker);
                    if (n4 == null) return node;
                    n4.AccessType |= AccessType.Public;
                    return node.Add(n4);
                case "static":
                    var n5 = SectionAssembleNode(walker);
                    if (n5 == null) return node;
                    n5.AccessType |= AccessType.Global;
                    return node.Add(n5);

                case "implements":
                case "extends":

                case "interface":
                case "class":
                case "enum":
                case "package":
                    var cname = walker.PeekProcedure();
                    if (cname != null && cname.Is(TokenType.Keyword))
                    {
                        (Compiler as Engine).SetKeyword(cname.Token.Update(TokenType.IdentifierKeyword));
                        walker.Replace(cname);
                    }
                    return node.AddRange(AssembleNode(walker), AssembleNode(walker));

                default:
                    return node.AddRange(AssembleNode(walker));

            }
        }
        protected override Node AssembleScopeNode(Node node, NodeWalker walker)
        {
            if (node.Is(TokenType.Start | TokenType.Scope))
            {
                var before = walker.PeekProcedure(-2);
                if (before != null &&
                    !before.Is(TokenType.End, TokenType.Statement, TokenType.Keyword, ~TokenType.DelimiterSymbol) &&
                    !before.IsMatch("=>")
                )
                    if (node.IsMatch("{"))
                        node.Token.Update(TokenType.ObjectData);
                    else if (node.IsMatch("[")) node.Token.Update(TokenType.ArrayData);
                    else node.Token.Update(TokenType.Scope);
                else node.Token.Update(TokenType.Scope);

                Location++;
                while (walker.Current != null && !walker.Current.Is(TokenType.End | TokenType.Scope))
                {
                    var n = SectionAssembleNode(walker);
                    if (n != null && !n.Is(NodeType.None)) node.Add(n);
                }
                if (node.IsMatch("{", "[", "(")) walker.Walk();
                else node.Add(AssembleNode(walker));
                Location--;
                return node;
            }
            else if (node.IsMatch("}", "]", ")"))
                return new Node();
            else if (node.Is(TokenType.End | TokenType.Scope))
                return node;
            else
                return node.Update(NodeType.BlockStructure).Add(SequenceAssembleNode(walker));
        }
        protected override Node AssembleDataNode(Node node, NodeWalker walker)
        {
            return node;
        }
        protected override Node AssembleSymbolNode(Node node, NodeWalker walker)
        {
            var next = walker.PeekProcedure();
            if (node.IsMatch("="))
                return node.Add(CompactAssembleNode(walker));
            if (node.Is(TokenType.ConcatenatorSymbol))
                return node.Add(AssembleNode(walker));
            if (node.Is(TokenType.DelimiterSymbol))
                return node.Add(CompactAssembleNode(walker));
            if (node.Is(TokenType.TerminatorSymbol) || node.Is(NodeType.Chunk, NodeType.Independ))
                if ((Compiler as Engine).IsComplementors(next))
                    return node.Add(CompactAssembleNode(walker));
                else return node;
            return node.Add(AssembleNode(walker));
        }
        protected override Node AssembleKeywordNode(Node node, NodeWalker walker)
        {
            if (node.Is(TokenType.NamespaceKeyword))
                return node.Add(CompactAssembleNode(walker));
            else if (node.Is(TokenType.FunctionKeyword))
                return node.Add(AssembleNode(walker));
            else return node;
        }
        protected override Node AssembleCommentNode(Node node, NodeWalker walker)
        {
            return Regex.IsMatch(node.Token.Value, @"^\s*\/{2}") ? node : AssemblePrefixNode(node, walker);
        }
        protected override Node AssembleStartNode(Node node, NodeWalker walker)
        {
            Location++;
            while (walker.Current != null && !walker.Current.Is(TokenType.End | TokenType.Scope))
            {
                var n = SectionAssembleNode(walker);
                if (n != null && !n.Is(NodeType.None)) node.Add(n);
            }
            node.Add(AssembleNode(walker));
            Location--;
            return node;
        }
        protected override Node AssemblePrefixNode(Node node, NodeWalker walker)
        {
            var child = CompactAssembleNode(walker);
            if ((Compiler as Engine).IsPrependent(node) && (Compiler as Engine).IsAppendent(child))
                return node.Add(child);
            else return new Node(new Token(), NodeType.Region, node, child);
        }
        protected override Node AssembleMiddleNode(Node node, NodeWalker walker)
        {
            var child = CompactAssembleNode(walker);
            if ((Compiler as Engine).IsPrependent(node) && (Compiler as Engine).IsAppendent(child))
                return node.Add(child);
            else return new Node(new Token(), NodeType.Region, node, child);
        }
        protected override Node AssembleSuffixNode(Node node, NodeWalker walker)
        {
            return node;
        }
        protected override Node AssembleEndNode(Node node, NodeWalker walker)
        {
            return node;
        }
        protected override Node AssembleUnknownNode(Node node, NodeWalker walker)
        {
            return node;
        }
    }
}
