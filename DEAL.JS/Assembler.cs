using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.DEAL.JS
{
    public class Assembler : MiMFa.Engine.JavaScript.Assembler
    {
        public new Engine Engine { get; set; }

        public int BreakParentSwitch { get; set; } = 0;
        public int BreakCollectSwitch { get; set; } = 0;
        public string[] EqualsSign => new string[] { "=", "=>", "->", ":" };

        public override bool Initialize(MiMFa.Engine.Engine engine)
        {
            if (engine != null) base.Initialize(Engine = engine as Engine);
            return true;
        }

        public override IEnumerable<Node> Assemble(NodeWalker walker, MiMFa.Engine.Engine compiler = null)
        {
            BreakParentSwitch = BreakCollectSwitch = 0;
            if (!Initialize(compiler)) return new Node[0];
            Assembled = new NodeWalker();
            while (!walker.IsEnded)
            {
                Location = 0;
                Assembled.Add(SectionAssembleNode(walker));
            }
            return Assembled.Content;
        }

        protected virtual Node QueryAssembleNode(NodeWalker walker)
        {
            return this.Engine.CreateLineNode(SectionAssembleNode(walker));
        }

        protected override Node SectionAssembleNode(NodeWalker walker)
        {
            return base.SectionAssembleNode(TrimSeparators(walker));
        }
        protected override IEnumerable<Node> SectionAssembleNodes(NodeWalker walker)
        {
            var en = SequenceAssembleNodes(walker).GetEnumerator();
            do
            {
                if (BreakParentSwitch > 0)
                {
                    BreakParentSwitch--;
                    break;
                }
                if (en.MoveNext()) yield return en.Current;
                else if (walker.Current != null && (walker.Current.Is(NodeType.Append) || !en.Current.Is(TokenType.TerminatorSymbol)))
                {
                    var next = walker.PeekProcedure();
                    if (next != null && !this.Engine.IsFlag(next))
                        if ((en.Current?.Is(NodeType.Prepend) == true && en.Current.Count <= 0) || this.Engine.IsComplementors(next))
                            foreach (var node in SectionAssembleNodes(walker))
                                yield return node;
                        else if (next.Is(TokenType.Symbol))
                            foreach (var node in SectionAssembleNodes(walker))
                                yield return node;
                    break;
                }
                else break;
            }
            while (true);
        }

        protected override Node SequenceAssembleNode(NodeWalker walker)
        {
            return this.Engine.TrimSeparators(base.SequenceAssembleNode(TrimSeparators(walker)));
        }
        protected override IEnumerable<Node> SequenceAssembleNodes(NodeWalker walker)
        {
            var en = CompactAssembleNodes(walker).GetEnumerator();
            do
            {
                if (BreakCollectSwitch > 0)
                {
                    BreakCollectSwitch--;
                    break;
                }
                if (BreakParentSwitch > 0)
                    break;
                if (en.MoveNext()) yield return en.Current;
                else if (walker.Current != null && (walker.Current.Is(NodeType.Append) || !this.Engine.IsFinalizers(en.Current)))
                {
                    var next = walker.PeekProcedure();
                    var next2 = walker.PeekProcedure(1);
                    if (next != null && !this.Engine.IsFinalizers(next) && !this.Engine.IsOrganizers(next) && !this.Engine.IsFlag(next))
                    {
                        if ((en.Current?.Is(NodeType.Prepend) == true && en.Current.Count <= 0) || this.Engine.IsMediators(next) || this.Engine.IsComplementors(next))
                            foreach (var node in SequenceAssembleNodes(walker))
                                yield return node;
                        else if (
                            this.Engine.IsDelimiters(next) &&
                            !this.Engine.IsComplementors(next2) &&
                            !this.Engine.IsFinalizers(next2) &&
                            !this.Engine.IsOrganizers(next2))
                            foreach (var node in SequenceAssembleNodes(walker))
                                yield return node;
                    }
                    break;
                }
                else break;
            }
            while (true);
        }

        protected override Node CompactAssembleNode(NodeWalker walker)
        {
            return this.Engine.TrimSeparators(base.CompactAssembleNode(TrimSeparators(walker)));
        }
        protected override IEnumerable<Node> CompactAssembleNodes(NodeWalker walker)
        {
            if (BreakCollectSwitch > 0)
                yield break;
            if (BreakParentSwitch > 0)
                yield break;
            var last = AssembleNode(walker);
            yield return last;
            if (BreakCollectSwitch > 0)
                yield break;
            if (BreakParentSwitch > 0)
                yield break;
            else if (walker.Current != null && (walker.Current.Is(NodeType.Append) || !this.Engine.IsFinalizers(last)))
            {
                var next = walker.PeekProcedure();
                if (next != null && !this.Engine.IsSeparators(next) && !this.Engine.IsFinalizers(next) && !this.Engine.IsOrganizers(next) && !this.Engine.IsFlag(next))
                    if ((last.Is(NodeType.Prepend) && last.Count <= 0) || this.Engine.IsConnectors(next) || this.Engine.IsMediators(next) || this.Engine.IsComplementors(next))
                        foreach (var node in CompactAssembleNodes(walker))
                            yield return node;
                    else if (
                        last != null &&
                        last.Token.Is(TokenType.Keyword) &&
                        !last.Is(NodeType.DefineStructure) &&
                        next.Is(TokenType.Keyword) &&
                        !this.Engine.IsSeparators(last.LastLeaf.Token) &&
                        !this.Engine.IsOrganizers(next)
                    )
                    {
                        yield return this.Engine.CreateNode(".");
                        foreach (var node in CompactAssembleNodes(walker))
                            yield return node;
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
                    var name = walker.Walk().Value;
                    if (this.Engine.IsDelimiters(next2) || next2?.IsMatch(EqualsSign) == true) walker.Remove(next2);
                    return Engine?.SetActionCommand(name,
                          this.Engine.CreateLineNode(
                              this.Engine.CreateCallNode(
                                 this.Engine.CreatePackNode(
                                     this.Engine.CreateDefineIdentifierNode(name,
                                         this.Engine.CreateCallableNode(
                                             this.Engine.TrimSeparators(
                                                 SectionAssembleNode(walker)
                                            )
                                         ), null
                                     )
                                 )
                            )
                         ).Update(NodeType.Region | NodeType.Line)
                     );

                case "command":
                    if (next?.IsMatch("(") == true)
                    {
                        node.Token.Value = "function";
                        break;
                    }
                    var cname = walker.WalkProcedure();
                    next = walker.PeekProcedure();
                    if (next?.IsMatch("(") == true)
                    {
                        node.Token.Value = "function";
                        Engine?.SetFunctionCommand(cname.Token.Update(TokenType.FunctionKeyword));
                        return node.Update(NodeType.DefineStructure | NodeType.Region | NodeType.Line | NodeType.Prepend).AddRange(
                            cname.Add(AssembleNode(walker)),
                            this.Engine.CreateBlockNode(SectionAssembleNode(walker.PeekProcedure()?.IsMatch(EqualsSign) == true ? walker.MoveToProcedure() : walker))
                        );
                    }
                    else
                    {
                        if (next.Is(TokenType.Middle | TokenType.Symbol))
                            walker.Walk();
                        Engine?.SetDefinitionCommand(cname.Token.Update(TokenType.IdentifierKeyword));
                        return this.Engine.CreateDefineIdentifierNode(cname.Token.Value, SectionAssembleNode(walker));
                    }

                case "implements":
                case "extends":
                    if (this.Engine.IsDelimiters(next)) walker.Walk();
                    return node.Update(NodeType.DefineStructure).AddRange(walker.Walk(), AssembleNode(walker));

                case "do":
                case "begin":
                case "doing":
                    if (node.Token.IsMatch("do") && next?.IsMatch("{") == true) break;
                    if (this.Engine.IsDelimiters(next)) walker.Walk();

                    Location++;
                    var doChildren = new List<Node>();
                    while (walker.IsRunning && !walker.Current.IsMatch("end"))
                    {
                        var n = SectionAssembleNode(walker);
                        if (!n.Is(NodeType.None)) doChildren.Add(n);
                    }
                    if (walker.IsRunning && walker.Current.IsMatch("end")) walker.Walk();
                    Location--;
                    if (this.Engine.IsSeparators(walker.Current))
                    {
                        BreakCollectSwitch++;
                        walker.Walk();
                    }
                    if (node.Token.IsMatch("doing"))
                        return this.Engine.CreateCallableNode(this.Engine.CreateBlockNode(doChildren.ToArray()));
                    else return this.Engine.CreateBlockNode(doChildren.ToArray());

                case "if":
                    if (next?.IsMatch("(") == true) break;
                    if (this.Engine.IsDelimiters(next)) walker.Walk();

                    var cond = CompactAssembleNode(walker);
                    var onTrue = CompactAssembleNode(walker);
                    var onFalse = (TrimSeparators(walker).PeekProcedure()?.IsMatch("else") == true) ? CompactAssembleNode(walker.MoveToProcedure()) : null;
                    bool isnormal = onFalse == null || (
                        this.Engine.IsGlobalNeeder(onTrue) ||
                        this.Engine.IsGlobalNeeder(onFalse) ||
                        before == null ||
                        this.Engine.IsFlag(before) ||
                        before.IsMatch("{", "do", "begin", "end", "else") ||
                        before.Is(TokenType.TerminatorSymbol)
                    );
                    if (isnormal) return node.Update(NodeType.ConditionStructure).AddRange(
                          cond, this.Engine.CreateLineNode(onTrue), onFalse == null? null: this.Engine.CreateLineNode(Engine.CreateNode("else", TokenType.Statement, NodeType.ConditionStructure, onFalse))
                      );
                    else return this.Engine.CreatePackNode(
                        cond.AddRange(Engine.CreateNode("?", TokenType.Symbol, NodeType.Depend, this.Engine.TrimSeparators(onTrue), Engine.CreateNode(":", TokenType.Symbol, NodeType.Depend, this.Engine.TrimSeparators(onFalse))))
                    );

                case "for":
                    if (next?.IsMatch("(") == true) break;
                    if (this.Engine.IsDelimiters(next)) walker.Walk();
                    if (
                        walker.PeekProcedure(1)?.IsMatch("of", "in") == true ||
                        walker.PeekProcedure(2)?.IsMatch("of", "in") == true ||
                        walker.PeekProcedure(3)?.IsMatch("of", "in") == true
                    )
                    {
                        if (walker.PeekProcedure(1).IsMatch("var", "const", "let"))
                            return node.Update(NodeType.IterationStructure).AddRange(
                                 this.Engine.CreatePackNode(CompactAssembleNode(walker)),
                                 CompactAssembleNode(walker)
                            );
                        else return node.Update(NodeType.IterationStructure).AddRange(
                            this.Engine.CreatePackNode(
                                this.Engine.CreateNode(
                                    "const",
                                    TokenType.Statement, NodeType.DefineStructure,
                                    CompactAssembleNode(walker)
                                )
                            ),
                                 CompactAssembleNode(walker)
                            );
                    }
                    else
                        return node.Update(NodeType.IterationStructure).AddRange(
                        this.Engine.CreatePackNode(
                            this.Engine.CreateNode("", this.Engine.TrimSeparators(CompactAssembleNode(walker)), this.Engine.CreateNode(";", TokenType.TerminatorSymbol)),
                            this.Engine.CreateNode("", this.Engine.TrimSeparators(CompactAssembleNode(walker)), this.Engine.CreateNode(";", TokenType.TerminatorSymbol)),
                            this.Engine.TrimSeparators(CompactAssembleNode(walker))
                        ),
                        CompactAssembleNode(walker)
                    );

                case "while":
                    if (next?.IsMatch("(") == true) break;
                    if (this.Engine.IsDelimiters(next)) walker.Walk();

                    return node.Update(NodeType.IterationStructure).AddRange(
                        this.Engine.CreatePackNode(CompactAssembleNode(walker)),
                         CompactAssembleNode(walker)
                     );

                case "try":
                    if (next?.IsMatch("{") == true) break;
                    if (this.Engine.IsDelimiters(next)) walker.Walk();

                    var children = new List<Node>();
                    while (walker.Current != null && !walker.Current.IsMatch("catch", "finally"))
                    {
                        var n = SectionAssembleNode(walker);
                        if (!n.Is(NodeType.None)) children.Add(n);
                    }
                    return node.Add(this.Engine.CreateBlockNode(children.ToArray()));
                case "catch":
                    if (next?.IsMatch("{") == true) break;
                    if (this.Engine.IsDelimiters(next)) walker.Walk();

                    if (next?.Is(TokenType.Keyword) == true && this.Engine.IsDelimiters(next2))
                        return node.AddRange(
                                this.Engine.CreatePackNode(this.Engine.TrimSeparators(CompactAssembleNode(walker))),
                                this.Engine.CreateBlockNode(SectionAssembleNode(walker.MoveToProcedure())),
                                walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? AssembleNode(walker) : null);
                    else return node.AddRange(
                             (next.IsMatch("(") ? new[] {
                                AssembleNode(walker),
                                this.Engine.CreateBlockNode(SectionAssembleNode(walker)),
                                walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? AssembleNode(walker) : null
                             } : new[] {
                                this.Engine.CreateBlockNode(SectionAssembleNode(walker)),
                                walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? AssembleNode(walker) : null}
                             )
                     );
                case "finally":
                    if (next?.IsMatch("{") == true) break;
                    if (this.Engine.IsDelimiters(next)) walker.Walk();

                    return node.AddRange(
                        this.Engine.CreateBlockNode(SectionAssembleNode(walker)),
                        walker.PeekProcedure()?.IsMatch("finally", "catch") == true ? AssembleNode(walker) : null
                    );
            }

            return base.AssembleStatementNode(node, walker);
        }
        protected override Node AssembleSymbolNode(Node node, NodeWalker walker)
        {
            var next = walker.PeekProcedure();
            if (Engine.IsSeparators(node))
            {
                if (
                    (Engine.IsConnectors(next) || Engine.IsComplementors(next) || Engine.IsMediators(next) || Engine.IsFinalizers(next)) &&
                    !next.Is(TokenType.Comment) &&
                    (!Engine.IsFlag(next) || !node.Is(TokenType.TerminatorSymbol))
                    )
                    return new Node();
            }
            if (next?.Is(TokenType.Symbol) == true && !this.Engine.IsInitializers(next))
                if (node.Is(TokenType.DelimiterSymbol))
                {
                    BreakCollectSwitch++;
                    return new Node();
                }
                else if (node.Is(TokenType.TerminatorSymbol))
                {
                    BreakParentSwitch++;
                    return new Node();
                }
                else
                {
                    node.Token.Clone(node.Token.Type | next.Token.Type, node.Value + next.Value, null);
                    return AssembleSymbolNode(node, walker.MoveToProcedure());
                }
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
                                    this.Engine.IsComplementors(next) ? null : this.Engine.CreateDotNode(),
                                    CompactAssembleNode(walker)
                                );
                        }
                    }
                }
                else if (!newNode.Is(TokenType.NamespaceKeyword))
                {
                    if (acname != null) return this.Engine.CreateNode(acname, NodeType.Chunk, TokenType.Data);
                    else
                    {
                        newNode.Token.Update(TokenType.IdentifierKeyword, fname ?? dcname ?? iname);
                        if (
                            next?.Is(TokenType.Keyword, TokenType.Data, TokenType.Start | TokenType.Scope) == true &&
                            !this.Engine.IsSeparators(next) &&
                            !this.Engine.IsMediators(next) &&
                            !this.Engine.IsComplementors(next) &&
                            !this.Engine.IsFinalizers(next) &&
                            !this.Engine.IsOrganizers(next) &&
                            (
                                fname != null ||
                                next.Is(TokenType.Data) ||
                                next.IsMatch("(", "...") ||
                                this.Engine.IsSeparators(next2) ||
                                this.Engine.IsMediators(next2) ||
                                this.Engine.IsComplementors(next2) ||
                                this.Engine.IsFinalizers(next2) ||
                                this.Engine.IsOrganizers(next2)
                            )
                        )
                        {
                            newNode.Token.Update(TokenType.FunctionKeyword);
                            return newNode.Update(NodeType.CallStructure)
                                .Add(next.IsMatch("(")? AssembleNode(walker) : Engine.CreatePackNode(SequenceAssembleNode(walker)));
                        }
                        else if (next?.Is(TokenType.Keyword)==true)
                        {
                            newNode.Token.Update(TokenType.NamespaceKeyword);
                            return newNode.Update(NodeType.CallStructure).AddRange(
                                this.Engine.IsComplementors(next) ? null : this.Engine.CreateDotNode(),
                                CompactAssembleNode(walker)
                            );
                        }
                        else if (fcname != null && (next == null || this.Engine.IsSeparators(next) || this.Engine.IsComplementors(next) || this.Engine.IsMediators(next) || this.Engine.IsFinalizers(next) || this.Engine.IsOrganizers(next)))
                            return this.Engine.CreateCallFunctionNode(fcname);
                        else if (this.Engine.IsDelimiters(next))
                        {
                            if (this.Engine.IsOrganizers(before))
                            {
                                walker.Remove(next);
                                return newNode.Update(NodeType.CallStructure);
                            }
                            else if (
                                next2?.Is(TokenType.Keyword) == true ||
                                this.Engine.IsConnectors(next2) ||
                                this.Engine.IsComplementors(next2) ||
                                this.Engine.IsMediators(next2) ||
                                this.Engine.IsFinalizers(next2) ||
                                this.Engine.IsOrganizers(next2)
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
            var type = node.Token.Value.Contains("\n") ? NodeType.Region :
                (node.Token.Value.StartsWith("/*") ? NodeType.Chunk : NodeType.Line);
            if (node.Token.Value.StartsWith("//") || this.Engine.IsFlag(walker.Current))
                return node.Update(node.Type|type);
            return new Node(new Token(TokenType.Unknown), type, node, CompactAssembleNode(walker));
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
                    else if (next.IsMatch("("))
                        return node.Update(NodeType.CallStructure).Add(AssembleNode(walker));
                    else if (next.Is(TokenType.Keyword) && (this.Engine.IsDelimiters(next2) || this.Engine.IsComplementors(next2) || this.Engine.IsOrganizers(next2)))
                        return node.Update(NodeType.CallStructure).Add(CompactAssembleNode(walker));
                    else
                    {
                        var child = CompactAssembleNode(walker);
                        if (child != null)
                            if (child.Is(NodeType.BlockStructure)) child = this.Engine.CreateCallableNode(child);
                            else if (!child.Is(NodeType.CallStructure) || child.Count > 0) child = this.Engine.CreateCallableNode(child);
                            else child = Engine.TrimSeparators(child);
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
                    Assembled.Insert(Assembled.Length - 1, this.Engine.CreateOpenBlockNode(), AssembleNode(walker), this.Engine.CreateNode(":", TokenType.Symbol, NodeType.Depend));
                    return this.Engine.CreateCloseBlockNode();
            }

            return base.AssembleMiddleNode(node, walker);
        }
        protected override Node AssembleSuffixNode(Node node, NodeWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            string dot = before == null || before.Value != "." ? "." : null;
            if (next == null || !next.Is(TokenType.Symbol) || next.Is(TokenType.ConcatenatorSymbol) || this.Engine.IsDelimiters(next))
                switch (node.Token.Value.ToLower())
                {
                    case "then":
                    case "otherwise":
                    case "anyway":
                        node.Token.Update(TokenType.FunctionKeyword, node.Token.IsMatch("otherwise") ? $"{dot}catch" : node.Token.IsMatch("anyway") ? $"{dot}finally" : $"{dot}then");
                        if (next == null) return new Node();
                        else if (next.IsMatch("("))
                            return node.Update(NodeType.CallStructure).Add(AssembleNode(walker));
                        else if (next.Is(TokenType.Keyword) && (this.Engine.IsSeparators(next2) || this.Engine.IsComplementors(next2) || this.Engine.IsFinalizers(next2) || this.Engine.IsOrganizers(next2)))
                            return node.Update(NodeType.CallStructure).Add(CompactAssembleNode(walker));
                        else
                        {
                            var child = CompactAssembleNode(walker);
                            if (child != null)
                                if (child.Is(NodeType.BlockStructure))
                                    child = this.Engine.CreateCallableNode(this.Engine.CreateLineNode(child.Insert(0, this.Engine.CreateNode("handle(data);"))), this.Engine.CreateNode("data", NodeType.Chunk, TokenType.Keyword));
                                else if (!child.Is(NodeType.CallStructure) || child.Count > 0)
                                    child = this.Engine.CreateCallableNode(this.Engine.CreateBlockNode(this.Engine.CreateNode("handle(data);"), this.Engine.CreateLineNode(child)), this.Engine.CreateNode("data", NodeType.Chunk, TokenType.Keyword));
                                else child = this.Engine.TrimSeparators(child);
                            return node.Update(NodeType.CallStructure).Add(child);
                        }

                    case "where":
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}filter");
                        return node.Update(NodeType.CallStructure).Add(this.Engine.CreateCallableNode(CompactAssembleNode(walker), this.Engine.CreateNode("data")));

                    case "distinct":
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}filter");
                        return node.Update(NodeType.CallStructure).Add(this.Engine.CreateCallableNode(this.Engine.CreateProceduresNode("", this.Engine.CreateNode("self.indexOf(data) === index"), this.Engine.CreateNode("data")), this.Engine.CreateNode("data")));

                    case "limit":
                        if (this.Engine.IsDelimiters(next)) walker.Walk();
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}slice");
                        var nlimit = SequenceAssembleNode(walker);
                        return node.Update(NodeType.CallStructure).AddRange(nlimit.Count > 1 ? nlimit.Children.ToArray() : new[] { this.Engine.CreateNode("0", NodeType.Chunk, TokenType.NumberData), nlimit });

                    case "order":
                        if (this.Engine.IsDelimiters(next)) walker.Walk();
                        var norders = SequenceAssembleNode(walker);
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
                            var comparatorNode = this.Engine.CreateNode(comparator, NodeType.Chunk, TokenType.Unknown);
                            childrenOrder.Add(this.Engine.CreateCallFunctionNode("sort", comparatorNode));
                        }

                        return this.Engine.CreateNode(dot, NodeType.Prepend, TokenType.Unknown, childrenOrder.ToArray());

                    case "keys":
                    case "values":
                        node.Token.Update(TokenType.FunctionKeyword, dot+node.Token.Value);
                        return node.Update(NodeType.CallStructure).AddRange(
                            this.Engine.CreatePackNode(),
                            Engine.CreateDotNode(
                                Engine.CreateNode("toArray", TokenType.FunctionKeyword, NodeType.CallStructure),
                                this.Engine.CreatePackNode()
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
