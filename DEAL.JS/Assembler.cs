using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.DEAL.JS
{
    public class Assembler : MiMFa.Engine.JavaScript.Assembler
    {
        public new Engine Engine { get; set; }

        public int LastIndex { get; set; } = -1;

        public int BreakSectionSwitch { get; set; } = 0;
        public int BreakSequenceSwitch { get; set; } = 0;
        public string[] AssignSigns => new string[] { "=", "=>", "->", ":" };

        public override bool Initialize(MiMFa.Engine.Engine engine)
        {
            if (engine != null) base.Initialize(Engine = engine as Engine);
            return true;
        }

        public override IEnumerable<Node> Assemble(NodeWalker walker, MiMFa.Engine.Engine compiler = null)
        {
            BreakSectionSwitch = BreakSequenceSwitch = 0;
            if (!Initialize(compiler)) return new Node[0];
            Assembled = new NodeWalker();
            while (!walker.IsEnded)
            {
                Location = 0;
                Assembled.Add(SectionAssembleNode(walker));
            }
            return Assembled.Content;
        }

        protected virtual Node TrimmedSectionAssembleNode(NodeWalker walker)
        {
            return Engine.TrimSeparators(SectionAssembleNode(TrimSeparators(walker)));
        }
        protected virtual Node NormalSectionAssembleNode(NodeWalker walker)
        {
            return base.SectionAssembleNode(TrimSeparators(walker));
        }
        protected override IEnumerable<Node> SectionAssembleNodes(NodeWalker walker)
        {
            var en = SequenceAssembleNodes(walker).GetEnumerator();
            do
            {
                if (BreakSectionSwitch > 0)
                {
                    BreakSectionSwitch--;
                    break;
                }
                if (en.MoveNext()) yield return en.Current;
                else if (walker.Current != null)
                {
                    var next = walker.Peek();
                    if (next != null && !Engine.IsFlag(next))
                        if (next.Is(NodeType.Append) && !Engine.IsSeparators(next))
                            foreach (var node in SectionAssembleNodes(walker))
                                yield return node;
                        else if(en.Current?.Is(TokenType.TerminatorSymbol) != true)
                        {
                            next = walker.PeekProcedure();
                            if (next != null && !Engine.IsFlag(next))
                                if (
                                    (en.Current?.Is(NodeType.Prepend) == true && en.Current.Count <= 0) || 
                                    Engine.IsComplementors(next)
                                )
                                    foreach (var node in SectionAssembleNodes(walker))
                                        yield return node;
                                else if (next.Is(TokenType.Symbol))
                                    foreach (var node in SectionAssembleNodes(walker))
                                        yield return node;
                        }
                    yield break;
                }
                else break;
            }
            while (true);
        }


        protected virtual Node TrimmedSequenceAssembleNode(NodeWalker walker)
        {
            return Engine.TrimSeparators(SequenceAssembleNode(walker));
        }
        protected virtual Node NormalSequenceAssembleNode(NodeWalker walker)
        {
            return base.SequenceAssembleNode(TrimSeparators(walker));
        }
        protected override IEnumerable<Node> SequenceAssembleNodes(NodeWalker walker)
        {
            var en = CompactAssembleNodes(walker).GetEnumerator();
            do
            {
                if (BreakSequenceSwitch > 0)
                {
                    BreakSequenceSwitch--;
                    yield break;
                }
                if (BreakSectionSwitch > 0)
                    if (LastIndex != walker.Index) yield break;
                if (en.MoveNext()) yield return en.Current;
                else if (walker.Current != null)
                {
                    var next = walker.Peek();
                    if (next != null && !Engine.IsFlag(next))
                        if (next.Is(NodeType.Append) && !Engine.IsSeparators(next))
                            foreach (var node in SequenceAssembleNodes(walker))
                                yield return node;
                        else if (en.Current?.Is(TokenType.End, TokenType.TerminatorSymbol) != true)
                        {
                            next = walker.PeekProcedure();
                            var next2 = walker.PeekProcedure(1);
                            if (next != null && !Engine.IsFlag(next) && !Engine.IsFinalizers(next) && !Engine.IsOrganizers(next))
                                if (
                                    (en.Current?.Is(NodeType.Prepend) == true && en.Current.Count <= 0) ||
                                    Engine.IsComplementors(next) ||
                                    Engine.IsMediators(next) ||
                                    en.Current?.Is(TokenType.DelimiterSymbol) == true
                                )
                                    foreach (var node in SequenceAssembleNodes(walker))
                                        yield return node;
                                else if (
                                    Engine.IsDelimiters(next) &&
                                    !Engine.IsComplementors(next2) &&
                                    !Engine.IsFinalizers(next2) &&
                                    !Engine.IsOrganizers(next2))
                                    foreach (var node in SequenceAssembleNodes(walker))
                                        yield return node;
                        }
                    yield break;
                }
                else yield break;
            }
            while (true);
        }

        protected virtual Node TrimmedCompactAssembleNode(NodeWalker walker)
        {
            return Engine.TrimSeparators(CompactAssembleNode(walker));
        }
        protected virtual Node NormalCompactAssembleNode(NodeWalker walker)
        {
            return base.CompactAssembleNode(TrimSeparators(walker));
        }
        protected override IEnumerable<Node> CompactAssembleNodes(NodeWalker walker)
        {
            if (BreakSequenceSwitch > 0)
                if(LastIndex != walker.Index) yield break;
            if (BreakSectionSwitch > 0)
                if (LastIndex != walker.Index) yield break;
            var latest = AssembleNode(walker);
            LastIndex = walker.Index;
            yield return latest;
            if (BreakSequenceSwitch > 0)
                yield break;
            if (BreakSectionSwitch > 0)
                yield break;

            var next = walker.Peek();
            if (next != null && !Engine.IsFlag(next))
            {
                if (next.Is(NodeType.Append) && !Engine.IsSeparators(next))
                    foreach (var node in CompactAssembleNodes(walker))
                        yield return node;
                else if (latest?.Is(TokenType.DelimiterSymbol, TokenType.End, TokenType.TerminatorSymbol) != true)
                {
                    next = walker.PeekProcedure();
                    if (next != null && !Engine.IsFlag(next) && !Engine.IsSeparators(next) && !Engine.IsFinalizers(next) && !Engine.IsOrganizers(next))
                        if (
                            (latest.Is(NodeType.Prepend) && latest.Count <= 0) ||
                            Engine.IsComplementors(next) ||
                            Engine.IsMediators(next) ||
                            Engine.IsConnectors(next)
                        )
                            foreach (var node in CompactAssembleNodes(walker))
                                yield return node;
                        else if (
                            next != null &&
                            !Engine.IsFlag(next) &&
                            next.Is(
                                TokenType.ConcatenatorSymbol,
                                TokenType.Suffix,
                                TokenType.Middle,
                                TokenType.End,
                                TokenType.Start | TokenType.Scope
                            ) &&
                            (
                                !Engine.IsSeparators(latest.LastLeaf) ||
                                (latest.LastLeaf.Parent != null && latest.LastLeaf.Ancestor(v => v.Is(NodeType.BlockStructure)) != null)
                            )
                        )
                            foreach (var node in CompactAssembleNodes(walker))
                                yield return node;
                        else if (
                            latest != null &&
                            (latest.Token.Is(TokenType.Keyword) || latest.Token.Is("[", "(")) &&
                            !latest.Is(NodeType.DefineStructure) &&
                            !Engine.IsSeparators(latest.LastLeaf.Token) &&
                            next.Is(TokenType.Keyword) &&
                            !Engine.IsOrganizers(next)
                        )
                        {
                            yield return Engine.CreateDotNode();
                            foreach (var node in CompactAssembleNodes(walker))
                                yield return node;
                        }
                }
            }
        }

        protected override Node AssembleStatementNode(Node node, NodeWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);

            switch (node.Token.Value.ToLower())
            {
                case "#":
                case "##":
                case "###":
                case "####":
                case "#####":
                case "######":
                    var name = walker.Walk();
                    if (Engine.IsDelimiters(next2) || next2?.Is(AssignSigns) == true) walker.Remove(next2);
                    Engine.SetActionCommand(name.Token);
                    return Engine.CreateLineNode(
                        Engine.CreateCallNode(
                            Engine.CreatePackNode(
                                Engine.CreateDefineIdentifierNode(name.Token.Value,
                                    Engine.CreateCallableNode(
                                        NormalSectionAssembleNode(walker)
                                    ), null
                                )
                            )
                         ).Update(NodeType.Region | NodeType.Line)
                     );

                case "command":
                    if (next?.Is("(") == true || (next.Is("*") && next2.Is("(")))
                    {
                        node.Token.Value = "function";
                        break;
                    }
                    var cname = walker.WalkProcedure();
                    if (cname != null && cname.Is("*"))
                    {
                        node.Token.Value += cname.Token.Value;
                        cname = walker.WalkProcedure();
                    }
                    next = walker.PeekProcedure();
                    if (next?.Is("(") == true)
                    {
                        node.Token.Value = "function";
                        Engine?.SetFunctionCommand(cname.Token.Update(TokenType.FunctionKeyword));
                        return node.Update(NodeType.DefineStructure | NodeType.Region | NodeType.Line | NodeType.Prepend).AddRange(
                            cname.Add(AssembleNode(walker)),
                            cname.Last.Count > 1 ? null : Engine.CreateReturnedBlockNode(NormalSectionAssembleNode(walker.PeekProcedure()?.Is(AssignSigns) == true ? walker.MoveToProcedure() : walker))
                        );
                    }
                    else
                    {
                        if (next?.Is(TokenType.Middle | TokenType.Symbol) == true)
                            walker.Walk();
                        var child = NormalSectionAssembleNode(walker);
                        Engine?.SetDefinitionCommand(cname.Token.Update(child.Is(TokenType.FunctionKeyword)? TokenType.FunctionKeyword: TokenType.IdentifierKeyword));
                        return Engine.CreateDefineIdentifierNode(cname.Token.Value, child);
                    }

                case "function":
                    if (next?.Is("(") == true || (next.Is("*") && next2.Is("("))) break;
                    var fname = walker.WalkProcedure();
                    if (fname != null && fname.Is("*"))
                    {
                        node.Token.Value += fname.Token.Value;
                        fname = walker.WalkProcedure();
                    }
                    Engine?.SetKeyword(fname.Token.Update(TokenType.FunctionKeyword));
                    return node.Update(NodeType.DefineStructure | NodeType.Region | NodeType.Line | NodeType.Prepend).AddRange(
                        fname.Add(AssembleNode(walker)),
                        fname.Last.Count > 1? null : Engine.CreateReturnedBlockNode(NormalSectionAssembleNode(walker.PeekProcedure()?.Is(AssignSigns) == true ? walker.MoveToProcedure() : walker))
                    );

                case "implements":
                case "extends":
                    if (Engine.IsDelimiters(next) || next?.Is(AssignSigns) == true) walker.Walk();
                    return node.Update(NodeType.DefineStructure).AddRange(walker.Walk(), AssembleNode(walker));

                case "do":
                case "begin":
                case "doing":
                    if (node.Token.Is("do") && next?.Is("{") == true) 
                        return node.AddRange(AssembleNode(walker), walker.WalkProcedure().Add(Engine.CreatePackNode(TrimmedCompactAssembleNode(walker))));
                    if (Engine.IsDelimiters(next) || next?.Is(AssignSigns) == true) walker.Walk();

                    Location++;
                    var doChildren = new List<Node>();
                    while (walker.IsRunning && !walker.Current.Is("end"))
                    {
                        var n = NormalSectionAssembleNode(walker);
                        if (!n.Is(NodeType.None)) doChildren.Add(n);
                    }
                    if (walker.IsRunning && walker.Current.Is("end")) walker.Walk();
                    Location--;
                    if (Engine.IsSeparators(walker.Current))
                    {
                        BreakSequenceSwitch++;
                        walker.Walk();
                    }
                    if (node.Token.Is("doing"))
                        return Engine.CreateCallableNode(Engine.CreateBlockNode(doChildren.ToArray()));
                    else return Engine.CreateBlockNode(doChildren.ToArray());

                case "if":
                    if (next?.Is("(") == true) break;
                    if (Engine.IsDelimiters(next) || next?.Is(AssignSigns) == true) walker.Walk();

                    var cond = Engine.CreatePackNode(TrimmedCompactAssembleNode(walker));
                    var onTrue = NormalSectionAssembleNode(walker);
                    var onFalse = (TrimSeparators(walker).PeekProcedure()?.Is("else") == true) ? NormalSectionAssembleNode(walker.MoveToProcedure()) : null;
                    bool isnormal = onFalse == null || (
                        Engine.IsGlobalNeeder(onTrue) ||
                        Engine.IsGlobalNeeder(onFalse) ||
                        before == null ||
                        Engine.IsFlag(before) ||
                        before.Is("{", "do", "begin", "end", "else") ||
                        before.Is(TokenType.TerminatorSymbol)
                    );
                    if (isnormal) return node.Update(NodeType.ConditionStructure).AddRange(
                          cond, Engine.CreateLineNode(onTrue), onFalse == null? null: Engine.CreateLineNode(Engine.CreateNode("else", TokenType.Statement, NodeType.ConditionStructure, onFalse))
                      );
                    else return Engine.CreatePackNode(
                        cond.AddRange(Engine.CreateNode("?", TokenType.Symbol | TokenType.Middle, NodeType.Depend, Engine.TrimSeparators(onTrue), Engine.CreateNode(":", TokenType.Symbol | TokenType.Middle, NodeType.Depend, Engine.TrimSeparators(onFalse))))
                    );

                case "for":
                    if (next?.Is("(") == true) break;
                    if (Engine.IsDelimiters(next) || next?.Is(AssignSigns) == true) walker.Walk();
                    if (
                        walker.PeekProcedure(1)?.Is("of", "in") == true ||
                        walker.PeekProcedure(2)?.Is("of", "in") == true ||
                        walker.PeekProcedure(3)?.Is("of", "in") == true
                    ) {
                        if (walker.PeekProcedure().Is("var", "const", "let"))
                            return node.Update(NodeType.IterationStructure).AddRange(
                                 Engine.CreatePackNode(TrimmedCompactAssembleNode(walker)),
                                 NormalSectionAssembleNode(walker)
                            );
                        else return node.Update(NodeType.IterationStructure).AddRange(
                            Engine.CreatePackNode(
                                Engine.CreateNode(
                                    "const",
                                    TokenType.Statement, NodeType.DefineStructure,
                                    NormalCompactAssembleNode(walker)
                                )
                            ),
                            NormalSectionAssembleNode(walker)
                        );
                    }
                    else
                        return node.Update(NodeType.IterationStructure).AddRange(
                        Engine.CreatePackNode(
                            Engine.CreateNode("", TrimmedCompactAssembleNode(walker), Engine.CreateNode(";", TokenType.TerminatorSymbol)),
                            Engine.CreateNode("", TrimmedCompactAssembleNode(walker), Engine.CreateNode(";", TokenType.TerminatorSymbol)),
                            TrimmedCompactAssembleNode(walker)
                        ),
                        NormalSectionAssembleNode(walker)
                    );

                case "while":
                    if (next?.Is("(") == true) break;
                    if (Engine.IsDelimiters(next) || next?.Is(AssignSigns) == true) walker.Walk();

                    return node.Update(NodeType.IterationStructure).AddRange(
                        Engine.CreatePackNode(TrimmedCompactAssembleNode(walker)),
                         NormalSectionAssembleNode(walker)
                     );

                case "try":
                    if (next?.Is("{") == true) break;
                    if (Engine.IsDelimiters(next) || next?.Is(AssignSigns) == true) walker.Walk();

                    var children = new List<Node>();
                    while (walker.Current != null && !walker.Current.Is("catch", "finally"))
                    {
                        var n = NormalSectionAssembleNode(walker);
                        if (!n.Is(NodeType.None)) children.Add(n);
                    }
                    return node.AddRange(Engine.CreateBlockNode(children.ToArray()), AssembleNode(walker));
                case "catch":
                    if (next?.Is("(", "{") == true) break;
                    if (Engine.IsDelimiters(next) || next?.Is(AssignSigns) == true)
                    {
                        walker.Walk();
                        next = walker.PeekProcedure();
                        next2 = walker.PeekProcedure(1);
                    }
                    if (next?.Is(TokenType.Keyword) == true && Engine.IsDelimiters(next2))
                        return node.AddRange(
                                Engine.CreatePackNode(TrimmedCompactAssembleNode(walker)),
                                Engine.CreateBlockNode(NormalSectionAssembleNode(walker)),
                                walker.PeekProcedure()?.Is("finally", "catch") == true ? AssembleNode(walker) : null);
                    else return node.AddRange(
                             (next.Is("(") ? new[] {
                                AssembleNode(walker),
                                Engine.CreateBlockNode(NormalSectionAssembleNode(walker)),
                                walker.PeekProcedure()?.Is("finally", "catch") == true ? AssembleNode(walker) : null
                             } : new[] {
                                Engine.CreateBlockNode(NormalSectionAssembleNode(walker)),
                                walker.PeekProcedure()?.Is("finally", "catch") == true ? AssembleNode(walker) : null}
                             )
                     );
                case "finally":
                    if (next?.Is("{") == true) break;
                    if (Engine.IsDelimiters(next) || next?.Is(AssignSigns) == true) walker.Walk();

                    return node.AddRange(
                        Engine.CreateBlockNode(NormalSectionAssembleNode(walker)),
                        walker.PeekProcedure()?.Is("finally", "catch") == true ? AssembleNode(walker) : null
                    );
            }

            return base.AssembleStatementNode(node, walker);
        }
        protected override Node AssembleSymbolNode(Node node, NodeWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();

            if (Engine.IsSeparators(node))
            {
                //if (
                //    (Engine.IsConnectors(next) || Engine.IsComplementors(next) || Engine.IsMediators(next) || Engine.IsFinalizers(next)) &&
                //    (!Engine.IsFlag(next) || !node.Is(TokenType.TerminatorSymbol))
                //)
                //    return new Node();
                if (Engine.IsOrganizers(next))
                {
                    node.Token.Update(TokenType.TerminatorSymbol, ";");
                    return node.Update(NodeType.Append);
                }
                else if (Engine.IsSeparators(next))
                    if (node.Is(TokenType.DelimiterSymbol))
                    {
                        BreakSequenceSwitch++;
                        return new Node();
                    }
                    else if (node.Is(TokenType.TerminatorSymbol))
                    {
                        BreakSectionSwitch++;
                        return new Node();
                    }
                    else return node.Update(NodeType.Append);
                else if (Engine.IsComplementors(next) || Engine.IsMediators(next))
                    return new Node();
            }
            else if (
                (Engine.IsAcceptors(before) || Engine.IsDelimiters(before) || Engine.IsInitializers(before) || Engine.IsOrganizers(before)) &&
                (Engine.IsSeparators(next) || Engine.IsComplementors(next) || Engine.IsMediators(next) || Engine.IsFinalizers(next) || Engine.IsOrganizers(next))
            )
            {
                node.Token.Update(TokenType.StringData, Newtonsoft.Json.JsonConvert.ToString(node.Token.Value));
                return node.Update(NodeType.Chunk);
            }

            //if (next?.Is(TokenType.Symbol) == true && !Engine.IsSeparators(next)  && !Engine.IsInitializers(next))
            //{
            //    node.Token.Clone(node.Token.Type | next.Token.Type, node.Value + next.Value, null);
            //    return AssembleSymbolNode(node, walker.MoveToProcedure());
            //}
            return base.AssembleSymbolNode(node, walker);
        }
        protected override Node AssembleKeywordNode(Node node, NodeWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            var newNode = base.AssembleKeywordNode(node, walker);

            if (newNode?.Is(NodeType.CallStructure) == true)
            {
                string fname = Engine?.GetFunctionName(newNode.Token.Value);
                string iname = fname != null ? null : Engine?.GetKeywordName(newNode.Token.Value);
                string fcname = Engine?.GetFunctionCommandName(newNode.Token.Value);
                string dcname = Engine?.GetDefinitionCommandName(newNode.Token.Value);
                string acname = Engine?.GetActionCommandName(newNode.Token.Value);
                fname = fname ?? fcname;
                if (newNode.Is(TokenType.FunctionKeyword))
                {
                    if (before?.Is(TokenType.ConcatenatorSymbol) == true) return newNode;
                    else
                    {
                        newNode.Token.Update(TokenType.FunctionKeyword, acname ?? fname);
                        if (!string.IsNullOrEmpty(acname) &&
                                next != null &&
                                next.Is(TokenType.Keyword)
                            )
                        {
                            newNode.Token.Update(TokenType.NamespaceKeyword);
                            return newNode
                                .Update(NodeType.CallStructure)
                                .AddRange(
                                    Engine.IsComplementors(next) ? null : Engine.CreateDotNode(),
                                    NormalCompactAssembleNode(walker)
                                );
                        }
                    }
                }
                else if (!newNode.Is(TokenType.NamespaceKeyword) && before?.Is("class", "interface", "enum", "implements", "extends") != true)
                {
                    if (acname != null) return Engine.CreateNode(acname, NodeType.Chunk, TokenType.Data);
                    else
                    {
                        newNode.Token.Update(TokenType.IdentifierKeyword, fname ?? dcname ?? iname);
                        if (
                            !newNode.Is("this") &&
                            iname == null &&
                            next?.Is(TokenType.Keyword, TokenType.Data, TokenType.Start | TokenType.Scope) == true &&
                            !Engine.IsSeparators(next) &&
                            !Engine.IsMediators(next) &&
                            !Engine.IsComplementors(next) &&
                            !Engine.IsFinalizers(next) &&
                            !Engine.IsOrganizers(next) &&
                            (
                                fname != null ||
                                next.Is(TokenType.Data) ||
                                next.Is("(", "...") ||
                                Engine.IsSeparators(next2) ||
                                Engine.IsMediators(next2) ||
                                Engine.IsComplementors(next2) ||
                                Engine.IsFinalizers(next2) ||
                                Engine.IsOrganizers(next2)
                            )
                        )
                        {
                            newNode.Token.Update(TokenType.FunctionKeyword);
                            return newNode.Update(NodeType.CallStructure)
                                .Add(next.Is("(") ? AssembleNode(walker) : Engine.CreatePackNode(NormalSequenceAssembleNode(walker)));
                        }
                        else if (next?.Is(TokenType.Keyword) == true)
                        {
                            newNode.Token.Update(TokenType.NamespaceKeyword);
                            return newNode.Update(NodeType.CallStructure).AddRange(
                                Engine.IsComplementors(next) ? null : Engine.CreateDotNode(),
                                NormalCompactAssembleNode(walker)
                            );
                        }
                        else if (fcname != null && (next == null || Engine.IsSeparators(next) || Engine.IsComplementors(next) || Engine.IsMediators(next) || Engine.IsFinalizers(next) || Engine.IsOrganizers(next)))
                            return Engine.CreateCallFunctionNode(fcname);
                        else if (Engine.IsDelimiters(next))
                        {
                            if (
                                next2?.Is(TokenType.Keyword) == true ||
                                Engine.IsConnectors(next2) ||
                                Engine.IsComplementors(next2) ||
                                Engine.IsMediators(next2) ||
                                Engine.IsFinalizers(next2) ||
                                Engine.IsOrganizers(next2)
                            )
                                return newNode.Update(NodeType.CallStructure);
                        }

                        return new Node(newNode.Token.Update(TokenType.IdentifierKeyword), NodeType.CallStructure);
                    }
                }
            }

            return node;
        }
        protected override Node AssembleCommentNode(Node node, NodeWalker walker)
        {
            var type = node.Token.Value.Contains("\n") ? NodeType.Region | NodeType.Line :
                (node.Token.Value.StartsWith("/*") ? NodeType.Chunk : NodeType.Line);
            if (node.Token.Value.StartsWith("//") || Engine.IsFlag(walker.Current))
                return node.Update(node.Type | type);
            return new Node(new Token(TokenType.Unknown), type, node, NormalCompactAssembleNode(walker));
        }
        protected override Node AssembleStartNode(Node node, NodeWalker walker)
        {
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            switch (node.Token.Value.ToLower())
            {
                case "will":
                    node.Token.Update(TokenType.FunctionKeyword, "new Promise");
                    if (next == null) return new Node();
                    else if (next.Is("("))
                        return node.Update(NodeType.CallStructure).Add(AssembleNode(walker));
                    else if (next.Is(TokenType.Keyword) && (Engine.IsDelimiters(next2) || Engine.IsComplementors(next2) || Engine.IsOrganizers(next2)))
                        return node.Update(NodeType.CallStructure).Add(NormalCompactAssembleNode(walker));
                    else
                    {
                        var child = NormalCompactAssembleNode(walker);
                        if (child != null)
                            if (child.Is(NodeType.BlockStructure)) child = Engine.CreateCallableNode(child);
                            else if (!child.Is(NodeType.CallStructure) || child.Count > 0) child = Engine.CreateCallableNode(child);
                        return node.Update(NodeType.CallStructure).Add(child);
                    }
            }
            return base.AssembleStartNode(node, walker);
        }
        protected override Node AssembleMiddleNode(Node node, NodeWalker walker)
        {
            switch (node.Token.Value.ToLower())
            {
                case "as":
                    Assembled.Insert(Assembled.Length - 1, Engine.CreateOpenBlockNode(), AssembleNode(walker), Engine.CreateNode(":", TokenType.Symbol, NodeType.Depend));
                    return Engine.CreateCloseBlockNode();
            }

            return base.AssembleMiddleNode(node, walker);
        }
        protected override Node AssembleSuffixNode(Node node, NodeWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            string dot = before == null || before.Token.Value != "." ? "." : null;
            if (next == null || !next.Is(TokenType.Symbol) || next.Is(TokenType.ConcatenatorSymbol) || Engine.IsDelimiters(next))
                switch (node.Token.Value.ToLower())
                {
                    case "then":
                    case "otherwise":
                    case "anyway":
                        node.Token.Update(TokenType.FunctionKeyword, node.Token.Is("otherwise") ? $"{dot}catch" : node.Token.Is("anyway") ? $"{dot}finally" : $"{dot}then");
                        if (next == null) return new Node();
                        else if (next.Is("("))
                            return node.Update(NodeType.CallStructure).Add(AssembleNode(walker));
                        else if (next.Is(TokenType.Keyword) && (Engine.IsSeparators(next2) || Engine.IsComplementors(next2) || Engine.IsFinalizers(next2) || Engine.IsOrganizers(next2)))
                            return node.Update(NodeType.CallStructure).Add(NormalCompactAssembleNode(walker));
                        else
                        {
                            var child = NormalCompactAssembleNode(walker);
                            if (child != null)
                                if (child.Is(NodeType.BlockStructure))
                                    child = Engine.CreateCallableNode(Engine.CreateLineNode(child.Insert(0, Engine.CreateNode("handle(data);"))), Engine.CreateNode("data", NodeType.Chunk, TokenType.Keyword));
                                else if (!child.Is(NodeType.CallStructure) || child.Count > 0)
                                    child = Engine.CreateCallableNode(Engine.CreateBlockNode(Engine.CreateNode("handle(data);"), Engine.CreateLineNode(child)), Engine.CreateNode("data", NodeType.Chunk, TokenType.Keyword));
                            return node.Update(NodeType.CallStructure).Add(child);
                        }

                    case "where":
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}filter");
                        return node.Update(NodeType.CallStructure).Add(Engine.CreateCallableNode(TrimmedCompactAssembleNode(walker), Engine.CreateNode("data")));

                    case "distinct":
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}filter");
                        return node.Update(NodeType.CallStructure).Add(Engine.CreateCallableNode(Engine.CreateProceduresNode("", Engine.CreateNode("self.indexOf(data) === index"), Engine.CreateNode("data")), Engine.CreateNode("data")));

                    case "limit":
                        if (Engine.IsDelimiters(next)) walker.Walk();
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}slice");
                        var nlimit = TrimmedSequenceAssembleNode(walker);
                        return node.Update(NodeType.CallStructure).AddRange(nlimit.Count > 1 ? nlimit.Children.ToArray() : new[] { Engine.CreateNode("0", NodeType.Chunk, TokenType.NumberData), nlimit });

                    case "order":
                        if (Engine.IsDelimiters(next)) walker.Walk();
                        var norders = TrimmedSequenceAssembleNode(walker);
                        var orderItems = norders.Is(NodeType.BlockStructure) ? norders.Children : new List<Node> { norders };
                        var childrenOrder = new List<Node>();

                        string KeyAccess(Node key, string prefix)
                        {
                            if (key == null) return prefix;
                            if (key.Token != null && !string.IsNullOrEmpty(key.Token.Value) && key.Children.Count == 0)
                                return prefix + "." + key.Token.Value;
                            // fallback: join child token values by dot
                            var parts = new List<string>();
                            if (!string.IsNullOrEmpty(key.Token?.Value)) parts.Add(key.Token.Value);
                            parts.AddRange(key.Children.Select(c => c.Token?.Value ?? c.ToString()));
                            return prefix + "." + string.Join(".", parts.Where(p => !string.IsNullOrEmpty(p)));
                        }

                        foreach (var item in orderItems)
                        {
                            // build comparator string: (a, b) => (a, b) => a.key > b.key ? 1 : a.key == b.key ? 0 : -1
                            var keyExprA = KeyAccess(item, "a");
                            var keyExprB = KeyAccess(item, "b");
                            var comparator = $"(a,b)=>(a,b)=>{keyExprA}>{keyExprB}?1:{keyExprA}=={keyExprB}?0:-1";
                            var comparatorNode = Engine.CreateNode(comparator, NodeType.Chunk, TokenType.Unknown);
                            childrenOrder.Add(Engine.CreateCallFunctionNode("sort", comparatorNode));
                        }

                        return Engine.CreateNode(dot, NodeType.Prepend, TokenType.Unknown, childrenOrder.ToArray());

                    case "keys":
                    case "values":
                        node.Token.Update(TokenType.FunctionKeyword, dot+node.Token.Value);
                        return node.Update(NodeType.CallStructure).AddRange(
                            Engine.CreatePackNode(),
                            Engine.CreateDotNode(
                                Engine.CreateNode("toArray", TokenType.FunctionKeyword, NodeType.CallStructure),
                                Engine.CreatePackNode()
                            )
                         );
                }

            return base.AssembleSuffixNode(node, walker);
        }


        public virtual NodeWalker TrimSeparators(NodeWalker walker)
        {
            return walker?.Current?.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol) == true ? walker.MoveToProcedure() : walker;
        }
    }
}
