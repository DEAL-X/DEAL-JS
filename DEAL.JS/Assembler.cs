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
        public string ArgumentName { get; set; } = "argument";
        public string[] AssignSigns => new string[] { "=", "==", "===", "=>", "->", ":", "as", "is", "be", "of", "in" };

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
                        else if (en.Current?.Is(TokenType.TerminatorSymbol) != true)
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
                if (LastIndex != walker.Index) yield break;
            if (BreakSectionSwitch > 0)
                if (LastIndex != walker.Index) yield break;
            var latest = AssembleNode(walker);
            LastIndex = walker.Index;
            if (walker.PeekProcedure()?.Is("as") == true)
                yield return Engine.CreatePairBlockNode(AssembleNode(walker.MoveToProcedure()), latest);
            else yield return latest;
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

        protected override Node AssembleNode(NodeWalker walker)
        {
            return base.AssembleNode(walker);
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
                    MoveToStartProcedure(ref walker);
                    Engine.SetActionCommand(name);
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
                    node.Token.Value = "function";
                    if (next?.Is("*") == true)
                    {
                        node.Token.Value += walker.WalkProcedure().Value;
                        next = walker.PeekProcedure();
                        next2 = walker.PeekProcedure(1);
                    }
                    if (next?.Is("(") == true)
                        return node.Update(NodeType.DefineStructure | NodeType.Region | NodeType.Line | NodeType.Prepend).AddRange(
                            AssembleNode(walker),
                            walker.PeekProcedure()?.Is(AssignSigns) == true ?
                            Engine.CreateReturnedBlockNode(
                                NormalSectionAssembleNode(
                                    MoveToStartProcedure(ref walker)
                                )
                            ) : AssembleNode(walker)
                        );
                    var cname = walker.WalkProcedure();
                    next = walker.PeekProcedure();
                    if (next?.Is("(") == true)
                    {
                        cname.Token.Type = TokenType.FunctionKeyword;
                        Engine?.SetFunctionCommand(cname);
                        return node.Update(NodeType.DefineStructure | NodeType.Region | NodeType.Line | NodeType.Prepend).AddRange(
                            cname.Add(AssembleNode(walker)),
                            cname.Last.Count > 1 && walker.PeekProcedure()?.Is("=>") != true ? null :
                                    walker.PeekProcedure()?.Is(AssignSigns) == true ?
                            Engine.CreateReturnedBlockNode(
                                NormalSectionAssembleNode(
                                        MoveToStartProcedure(ref walker)
                                )
                            ) : AssembleNode(walker)
                        );
                    }
                    else
                    {
                        if (next?.Is(TokenType.Middle | TokenType.Symbol) == true)
                            walker.Walk();
                        var child = NormalSectionAssembleNode(walker);
                        cname.Token.Type = child.Is(TokenType.FunctionKeyword) ? TokenType.FunctionKeyword : TokenType.IdentifierKeyword;
                        Engine?.SetDefinitionCommand(cname);
                        return Engine.CreateDefineIdentifierNode(cname.Token.Value, child);
                    }

                case "function":
                    if (next?.Is("*") == true)
                    {
                        node.Token.Value += walker.WalkProcedure().Value;
                        next = walker.PeekProcedure();
                        next2 = walker.PeekProcedure(1);
                    }
                    if (next?.Is("(") == true)
                        return node.Update(NodeType.DefineStructure | NodeType.Region | NodeType.Line | NodeType.Prepend).AddRange(
                            AssembleNode(walker),
                            walker.PeekProcedure()?.Is(AssignSigns) == true ?
                            Engine.CreateReturnedBlockNode(
                                NormalSectionAssembleNode(
                                    MoveToStartProcedure(ref walker)
                                )
                            ) : AssembleNode(walker)
                        );
                    var fname = walker.WalkProcedure();
                    fname.Token.Type = TokenType.FunctionKeyword;
                    Engine?.Keywords.Set(fname);
                    return node.Update(NodeType.DefineStructure | NodeType.Region | NodeType.Line | NodeType.Prepend).AddRange(
                        fname.Add(AssembleNode(walker)),
                        fname.Last.Count > 1 && walker.PeekProcedure()?.Is("=>") != true ? null :
                        walker.PeekProcedure()?.Is(AssignSigns) == true ? 
                        Engine.CreateReturnedBlockNode(
                            NormalSectionAssembleNode(
                                MoveToStartProcedure(ref walker)
                            )
                        ) : AssembleNode(walker)
                    );

                case "implements":
                case "extends":
                    MoveToStartProcedure(ref walker);
                    return node.Update(NodeType.DefineStructure).AddRange(walker.Walk(), AssembleNode(walker));

                case "do":
                case "begin":
                case "doing":
                    if (node.Token.Is("do") && next?.Is("{") == true)
                        return node.AddRange(AssembleNode(walker), walker.WalkProcedure().Add(Engine.CreatePackNode(TrimmedCompactAssembleNode(walker))));
                    MoveToStartProcedure(ref walker);

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
                    MoveToStartProcedure(ref walker);

                    var cond = Engine.CreatePackNode(TrimmedCompactAssembleNode(walker));
                    var onTrue = NormalSectionAssembleNode(MoveToStartProcedure(ref walker));
                    Node onFalse = null;
                    if (TrimSeparators(walker).PeekProcedure()?.Is("else") == true)
                    {
                        walker.MoveToProcedure();
                        MoveToStartProcedure(ref walker);
                        onFalse = NormalSectionAssembleNode(walker);
                    }
                    bool isnormal = onFalse == null || (
                        Engine.IsGlobalNeeder(onTrue) ||
                        Engine.IsGlobalNeeder(onFalse) ||
                        before == null ||
                        Engine.IsFlag(before) ||
                        before.Is("{", "do", "begin", "end", "else") ||
                        before.Is(TokenType.TerminatorSymbol)
                    );
                    if (isnormal) 
                        if(onFalse == null) return node.Update(NodeType.ConditionStructure).AddRange(
                            cond, Engine.CreateLineNode(onTrue)
                        );
                        else return node.Update(NodeType.ConditionStructure).AddRange(
                            cond, Engine.CreateLineNode(onTrue), Engine.CreateLineNode(Engine.CreateNode("else", TokenType.Statement, NodeType.ConditionStructure, onFalse))
                         );
                    else return Engine.CreatePackNode(
                        cond.AddRange(Engine.CreateNode("?", TokenType.Symbol | TokenType.Middle, NodeType.Depend, Engine.TrimSeparators(onTrue), Engine.CreateNode(":", TokenType.Symbol | TokenType.Middle, NodeType.Depend, Engine.TrimSeparators(onFalse))))
                    );

                case "for":
                    if (next?.Is("(") == true) break;
                    MoveToStartProcedure(ref walker);
                    if (
                        walker.PeekProcedure(1)?.Is("of", "in") == true ||
                        walker.PeekProcedure(2)?.Is("of", "in") == true ||
                        walker.PeekProcedure(3)?.Is("of", "in") == true
                    )
                    {
                        if (walker.PeekProcedure().Is("var", "const", "let"))
                            return node.Update(NodeType.IterationStructure).AddRange(
                                 Engine.CreatePackNode(TrimmedCompactAssembleNode(walker)),
                                 NormalSectionAssembleNode(MoveToStartProcedure(ref walker))
                            );
                        else return node.Update(NodeType.IterationStructure).AddRange(
                            Engine.CreatePackNode(
                                Engine.CreateNode(
                                    "const",
                                    TokenType.Statement, NodeType.DefineStructure,
                                    NormalCompactAssembleNode(walker)
                                )
                            ),
                            NormalSectionAssembleNode(MoveToStartProcedure(ref walker))
                        );
                    }
                    else
                        return node.Update(NodeType.IterationStructure).AddRange(
                        Engine.CreatePackNode(
                            Engine.CreateNode("", TrimmedCompactAssembleNode(walker), Engine.CreateNode(";", TokenType.TerminatorSymbol)),
                            Engine.CreateNode("", TrimmedCompactAssembleNode(walker), Engine.CreateNode(";", TokenType.TerminatorSymbol)),
                            TrimmedCompactAssembleNode(walker)
                        ),
                        NormalSectionAssembleNode(MoveToStartProcedure(ref walker))
                    );

                case "while":
                    if (next?.Is("(") == true) break;
                    MoveToStartProcedure(ref walker);

                    return node.Update(NodeType.IterationStructure).AddRange(
                        Engine.CreatePackNode(TrimmedCompactAssembleNode(walker)),
                         NormalSectionAssembleNode(MoveToStartProcedure(ref walker))
                     );

                case "consider":
                    string ntype = "let";
                    if (next?.Is("const", "constant") == true)
                    {
                        node = walker.Walk();
                        ntype = "const";
                    }
                    else if (next?.Is("var", "variable") == true)
                    {
                        node = walker.Walk();
                        ntype = "var";
                    }
                    else if (next?.Is("let") == true)
                    {
                        node = walker.Walk();
                        ntype = "let";
                    }
                    MoveToStartProcedure(ref walker);

                    var nname = walker.Walk();
                    next = walker.PeekProcedure();

                    if (next?.Is("const", "constant") == true)
                    {
                        node = walker.Walk();
                        ntype = "const";
                    }
                    else if (next?.Is("var", "variable") == true)
                    {
                        node = walker.Walk();
                        ntype = "var";
                    }
                    else if (next?.Is("let") == true)
                    {
                        node = walker.Walk();
                        ntype = "let";
                    }
                    MoveToStartProcedure(ref walker);

                    node.Token.Update(TokenType.Statement, ntype);
                    node.Add(nname); // Add the Name
                    //Engine.SetKeyword(nname);
                    return node.Add(Engine.CreateNode("=", TokenType.Symbol, NodeType.Depend, NormalSectionAssembleNode(walker)));

                case "try":
                    if (next?.Is("{") == true) break;
                    MoveToStartProcedure(ref walker);

                    var children = new List<Node>();
                    while (walker.Current != null && !walker.Current.Is("catch", "finally"))
                    {
                        var n = NormalSectionAssembleNode(walker);
                        if (!n.Is(NodeType.None)) children.Add(n);
                    }
                    return node.AddRange(Engine.CreateBlockNode(children.ToArray()), AssembleNode(walker));
                case "catch":
                    if (next?.Is("(", "{") == true) break;
                    MoveToStartProcedure(ref walker);
                    next = walker.PeekProcedure();
                    next2 = walker.PeekProcedure(1);
                    if (next?.Is(TokenType.Keyword) == true && Engine.IsDelimiters(next2))
                        return node.AddRange(
                                Engine.CreatePackNode(TrimmedCompactAssembleNode(walker)),
                                Engine.CreateBlockNode(NormalSectionAssembleNode(MoveToStartProcedure(ref walker))),
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
                    MoveToStartProcedure(ref walker);

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

            if (node.Is("is"))
            {
                if (node.Count <= 0 && before?.Is(TokenType.Keyword) == true && next?.Is(TokenType.Keyword) == true)
                {
                    node.Token.Update(TokenType.Suffix, "."+node.Token.Value + next.Value);
                    node.Type = NodeType.Append;
                    return AssembleSuffixNode(node, walker.MoveToProcedure());
                }
                else
                {
                    node.Token.Value = "==";
                    return base.AssembleSymbolNode(node, walker);
                }
            }
            else if (Engine.IsSeparators(node))
            {
                //if (
                //    (Engine.IsConnectors(next) || Engine.IsComplementors(next) || Engine.IsMediators(next) || Engine.IsFinalizers(next)) &&
                //    (!Engine.IsFlag(next) || !node.Is(TokenType.TerminatorSymbol))
                //)
                //    return new Node();
                if (next?.Token?.Type == TokenType.Statement)
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
            else if (Engine.IsMediators(node) && Engine.IsSeparators(next))
                return base.AssembleSymbolNode(node, walker.MoveToProcedure());

            //if (next?.Is(TokenType.Symbol) == true && !Engine.IsSeparators(next)  && !Engine.IsInitializers(next))
            //{
            //    node.Token.Clone(node.Token.Type | next.Token.Type, node.Value + next.Value, null);
            //    return AssembleSymbolNode(node, walker.MoveToProcedure());
            //}
            return base.AssembleSymbolNode(node, walker);
        }
        private void FindHierarchicalCommandItem(
            MiMFa.Engine.JavaScript.Keywords keyboard,
            NodeWalker walker,
            int offset,
            ref MiMFa.Engine.JavaScript.Keywords result,
            ref int consumed
        )
        {
            if (keyboard == null || keyboard.Count == 0) return;

            var next = walker.PeekProcedure(offset);
            if (next == null) return;

            int step = 1;
            if (next.Is(TokenType.ConcatenatorSymbol))
            {
                next = walker.PeekProcedure(offset + 1);
                if (next == null) return;
                step++;
            }
            if (!next.Is(TokenType.Keyword, TokenType.IdentifierKeyword, TokenType.FunctionKeyword, TokenType.NamespaceKeyword)) return;

            var child = keyboard.Get(next.Value.ToLower());
            if (child == null) return;

            consumed = offset + step;
            result = child;
            FindHierarchicalCommandItem(child, walker, offset + step, ref result, ref consumed);
        }

        private MiMFa.Engine.JavaScript.Keywords FindHierarchicalCommand(
            MiMFa.Engine.JavaScript.Keywords keyboard,
            string name,
            NodeWalker walker,
            out int consumed
        )
        {
            consumed = 0;
            if (keyboard == null || string.IsNullOrWhiteSpace(name)) return null;

            var names = name.Split(new[] { '.' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(value => value.ToLower())
                .ToArray();
            var current = keyboard.Get(names);
            if (current == null) return null;

            var result = current;
            FindHierarchicalCommandItem(current, walker, 0, ref result, ref consumed);
            return result;
        }

        protected override Node AssembleKeywordNode(Node node, NodeWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            bool isNamespaced = before?.Is(TokenType.IdentifierKeyword, TokenType.NamespaceKeyword, TokenType.ConcatenatorSymbol) == true;
            string nvalue = isNamespaced ? "" : node.ToString("");

            var fKeyword = Engine?.GetFunction(nvalue);
            var iKeyword = fKeyword != null ? null : Engine?.Keywords.Get(nvalue);
            int functionCommandLength, definitionCommandLength, actionCommandLength;
            var fcKeyword = FindHierarchicalCommand(Engine?.FunctionCommands, nvalue, walker, out functionCommandLength);
            var dcKeyword = FindHierarchicalCommand(Engine?.DefinitionCommands, nvalue, walker, out definitionCommandLength);
            var acKeyword = FindHierarchicalCommand(Engine?.ActionCommands, nvalue, walker, out actionCommandLength);

            int commandLength = System.Math.Max(functionCommandLength, System.Math.Max(definitionCommandLength, actionCommandLength));
            if (commandLength > 0)
            {
                if (actionCommandLength == commandLength && acKeyword != null) { fcKeyword = null; dcKeyword = null; }
                else if (functionCommandLength == commandLength && fcKeyword != null) { dcKeyword = null; acKeyword = null; }
                else if (definitionCommandLength == commandLength && dcKeyword != null) { fcKeyword = null; acKeyword = null; }
                for (int index = 0; index < commandLength; index++) walker.MoveToProcedure();
                next = walker.PeekProcedure();
                next2 = walker.PeekProcedure(1);
            }

            fKeyword = fKeyword ?? fcKeyword;
            bool isDirect = node.Value.Contains(".");
            bool isFunctionCommand = !isNamespaced && fcKeyword != null;
            bool isDefinitionCommand = !isNamespaced && dcKeyword != null;
            bool isActionCommand = !isNamespaced && acKeyword != null;
            bool isCommand = isFunctionCommand || isDefinitionCommand || isActionCommand;
            bool isCallable = fKeyword != null;
            bool isCommandBoundary = next == null || Engine.IsSeparators(next) || Engine.IsComplementors(next) ||
                Engine.IsMediators(next) || Engine.IsFinalizers(next) || Engine.IsOrganizers(next);
            bool isArgumentStart = next?.Is(TokenType.Data, TokenType.Start | TokenType.Scope) == true ||
                (isCallable && next?.Is(TokenType.Keyword) == true);
            bool isDelimiterContinuation = next2?.Is(TokenType.Keyword) == true || Engine.IsConnectors(next2) ||
                Engine.IsComplementors(next2) || Engine.IsMediators(next2) ||
                Engine.IsFinalizers(next2) || Engine.IsOrganizers(next2);

            if (next?.Is(":") == true)
                return new Node(node.Token.Update(TokenType.IdentifierKeyword), NodeType.DefineStructure);
            if (node.Is(TokenType.NamespaceKeyword))
                if (next?.Is(TokenType.ConcatenatorSymbol) == true || !next?.Is(TokenType.Keyword) == true)
                    return node.Add(CompactAssembleNode(walker));
                else return node.Add(Engine.CreateDotNode(CompactAssembleNode(walker)));
            else if (node.Is(TokenType.FunctionKeyword))
            {
                if (next?.Is("(") == true)
                {
                    node.Add(AssembleNode(walker));
                    if (walker.PeekProcedure()?.Is("{") == true)
                        return node.Update(NodeType.DefineStructure).Add(AssembleNode(walker));
                    else if (isCommand) node.Token.Value = acKeyword?.Name ?? fKeyword?.Name ?? node.Token.Value;
                    return node;
                }
                else if (before?.Is(TokenType.ConcatenatorSymbol) == true) return node;
                else if (next?.Is(TokenType.ConcatenatorSymbol) == true)
                {
                    node.Token.Type = TokenType.NamespaceKeyword;
                    return node;
                }
                else
                {
                    if (!isNamespaced) node.Token.Value = acKeyword?.Name ?? fKeyword?.Name ?? node.Token.Value;
                    if (acKeyword != null && next?.Is(TokenType.Keyword) == true)
                    {
                        node.Token.Update(TokenType.NamespaceKeyword);
                        return node
                            .Update(NodeType.CallStructure)
                            .AddRange(
                                Engine.IsComplementors(next) ? null : Engine.CreateDotNode(),
                                NormalCompactAssembleNode(walker)
                            );
                    }
                    else if (next?.Is(TokenType.Symbol) != true && !isDirect) node.Add(Engine.CreatePackNode(TrimmedSequenceAssembleNode(walker)));
                }
            }
            else if (before?.Is("class", "interface", "enum", "implements", "extends") != true)
            {
                if (acKeyword != null) return Engine.CreateNode(acKeyword.Name, TokenType.Data, NodeType.Chunk);
                else
                {
                    if (!isNamespaced) node.Token.Update(TokenType.IdentifierKeyword, (fKeyword ?? dcKeyword ?? iKeyword)?.Name);

                    if (
                        !node.Is("this") &&
                        isArgumentStart &&
                        (iKeyword == null || before?.Is(TokenType.IdentifierKeyword, TokenType.NamespaceKeyword, TokenType.ConcatenatorSymbol) == true) &&
                        !Engine.IsSeparators(next) &&
                        !Engine.IsMediators(next) &&
                        !Engine.IsComplementors(next) &&
                        !Engine.IsFinalizers(next) &&
                        !Engine.IsOrganizers(next) &&
                        (
                            (!isNamespaced && fKeyword != null) ||
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
                        node.Token.Update(TokenType.FunctionKeyword);
                        return node.Update(NodeType.CallStructure)
                            .Add(next.Is("(") ? AssembleNode(walker) : Engine.CreatePackNode(NormalSequenceAssembleNode(walker)));
                    }
                    else if (next?.Is(TokenType.Keyword) == true)
                    {
                        node.Update(NodeType.CallStructure).AddRange(
                            Engine.IsComplementors(next) ? null : Engine.CreateDotNode(),
                            NormalCompactAssembleNode(walker)
                        );
                        node.Token.Update(TokenType.NamespaceKeyword);
                        if (before?.Is(TokenType.NamespaceKeyword) == true && before?.Count < 1)
                            return AssembleKeywordNode(node, walker);
                        else return node;
                    }
                    if (isFunctionCommand && isCommandBoundary)
                        return Engine.CreateCallFunctionNode(fcKeyword.Name);
                    else if (Engine.IsDelimiters(next) && isDelimiterContinuation)
                        return node.Update(NodeType.CallStructure);

                    return new Node(node.Token.Update(TokenType.IdentifierKeyword), NodeType.CallStructure);
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
        protected override Node AssemblePrefixNode(Node node, NodeWalker walker)
        {
            if (walker.PeekProcedure()?.Is(":") == true)
                return new Node(node.Token.Update(TokenType.IdentifierKeyword), NodeType.DefineStructure);
            return base.AssemblePrefixNode(node, walker);
        }
        protected override Node AssembleStartNode(Node node, NodeWalker walker)
        {
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            if (next?.Is(":") == true)
                return new Node(node.Token.Update(TokenType.IdentifierKeyword), NodeType.DefineStructure);
            switch (node.Token.Value.ToLower())
            {
                case "will":
                    node.Token.Update(TokenType.FunctionKeyword, "new Promise");
                    if (next == null) return new Node();
                    MoveToStartProcedure(ref walker);
                    next = walker.PeekProcedure();
                    next2 = walker.PeekProcedure(1);
                    if (next.Is("("))
                        return node.Update(NodeType.CallStructure).Add(AssembleNode(walker));
                    else if (next.Is(TokenType.Keyword) && (Engine.IsDelimiters(next2) || Engine.IsComplementors(next2) || Engine.IsOrganizers(next2)))
                        return node.Update(NodeType.CallStructure).Add(Engine.CreatePackNode(NormalCompactAssembleNode(walker)));
                    else
                    {
                        var child = NormalCompactAssembleNode(walker);
                        if (child != null)
                            if (child.Is(NodeType.BlockStructure)) child = Engine.CreateCallableNode(child, Engine.CreateNode("resolve, reject", NodeType.Chunk));
                            else if (!child.Is(NodeType.CallStructure) || child.Count > 0)
                                child = Engine.CreateCallableNode(
                                    child.Value.ToLower().Contains("resolve") ||
                                    child.Value.ToLower().Contains("reject") ?
                                        child :
                                        Engine.CreateCallFunctionNode("resolve", child),
                                        Engine.CreateNode("resolve, reject", NodeType.Chunk)
                                );
                        return node.Update(NodeType.CallStructure).Add(child);
                    }
            }
            return base.AssembleStartNode(node, walker);
        }
        protected override Node AssembleMiddleNode(Node node, NodeWalker walker)
        {
            if (walker.PeekProcedure()?.Is(":") == true)
                return new Node(node.Token.Update(TokenType.IdentifierKeyword), NodeType.DefineStructure);
            return base.AssembleMiddleNode(node, walker);
        }
        protected override Node AssembleEndNode(Node node, NodeWalker walker)
        {
            if (walker.PeekProcedure()?.Is(":") == true)
                return new Node(node.Token.Update(TokenType.IdentifierKeyword), NodeType.DefineStructure);
            return base.AssembleEndNode(node, walker);
        }
        protected override Node AssembleSuffixNode(Node node, NodeWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            string dot = before == null || before.Token.Value != "." ? "." : null;
            if (
                before != null &&
                (
                    (
                        !before.Is(TokenType.Start | TokenType.Scope) &&
                        !(Engine as Engine).IsOrganizers(before)
                    ) 
                    &&
                    (
                        next == null ||
                        !next.Is(AssignSigns) && 
                        (
                            !next.Is(TokenType.Symbol) ||
                            Engine.IsDelimiters(next)
                        )
                    )
                )
            )
                switch (node.Token.Value.ToLower())
                {
                    case "then":
                    case "otherwise":
                    case "anyway":
                        string handler = $"handle({ArgumentName});";
                        if (node.Token.Is("otherwise"))
                        {
                            handler = $"handle({ArgumentName}, false);";
                            node.Token.Update(TokenType.FunctionKeyword, $"{dot}catch");
                        }
                        else if (node.Token.Is("anyway"))
                        {
                            handler = $"handle({ArgumentName});";
                            node.Token.Update(TokenType.FunctionKeyword, $"{dot}finally");
                        }
                        else
                        {
                            handler = $"handle({ArgumentName}, true);";
                            node.Token.Update(TokenType.FunctionKeyword, $"{dot}then");
                        }

                        if (next == null) return new Node();
                        if (Engine.IsDelimiters(next) || next.Is(AssignSigns))
                        {
                            walker.Walk();
                            next = walker.PeekProcedure();
                            next2 = walker.PeekProcedure(1);
                        }
                        if (next.Is("("))
                            return node.Update(NodeType.CallStructure).Add(AssembleNode(walker));
                        else if (next.Is(TokenType.Keyword) && (Engine.IsSeparators(next2) || Engine.IsComplementors(next2) || Engine.IsFinalizers(next2) || Engine.IsOrganizers(next2)))
                            return node.Update(NodeType.CallStructure).Add(Engine.CreatePackNode(NormalCompactAssembleNode(walker)));
                        else
                        {
                            var child = NormalCompactAssembleNode(walker);
                            if (child != null)
                                if (child.Is(NodeType.BlockStructure))
                                    child = Engine.CreateCallableNode(Engine.CreateLineNode(child.Insert(0, Engine.CreateNode(handler))), Engine.CreateNode(ArgumentName, NodeType.Chunk, TokenType.Keyword));
                                else if (!child.Is(NodeType.CallStructure) || child.Count > 0)
                                    child = Engine.CreateCallableNode(Engine.CreateBlockNode(Engine.CreateNode(handler), Engine.CreateLineNode(child)), Engine.CreateNode(ArgumentName, NodeType.Chunk, TokenType.Keyword));
                            return node.Update(NodeType.CallStructure).Add(child);
                        }

                    case "where":
                        MoveToStartProcedure(ref walker);
                        next = walker.PeekProcedure();
                        next2 = walker.PeekProcedure(1);
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}filter");
                        return node.Update(NodeType.CallStructure).Add(
                            Engine.CreatePackNode(
                                Engine.CreateCallableNode(
                                    TrimmedCompactAssembleNode(walker),
                                    Engine.CreateNode(ArgumentName)
                                    )
                                )
                            );

                    case "distinct":
                        MoveToStartProcedure(ref walker);
                        next = walker.PeekProcedure();
                        next2 = walker.PeekProcedure(1);
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}filter");
                        return node.Update(NodeType.CallStructure).Add(
                            Engine.CreatePackNode(
                                Engine.CreateCallableNode(
                                    Engine.CreateProceduresNode("", Engine.CreateNode($"self.indexOf({ArgumentName}) === index"),
                                    Engine.CreateNode(ArgumentName)),
                                    Engine.CreateNode(ArgumentName)
                                    )
                                )
                            );

                    case "limit":
                        MoveToStartProcedure(ref walker);
                        next = walker.PeekProcedure();
                        next2 = walker.PeekProcedure(1);
                        node.Token.Update(TokenType.FunctionKeyword, $"{dot}slice");
                        var nlimit = TrimmedSequenceAssembleNode(walker);
                        return node.Update(NodeType.CallStructure).Add(
                            Engine.CreatePackNode(
                                nlimit.Count > 1 ? nlimit.Children.ToArray() : new[] { Engine.CreateNode("0", NodeType.Chunk, TokenType.NumberData), nlimit }
                            )
                            );

                    case "order":
                        MoveToStartProcedure(ref walker);
                        next = walker.PeekProcedure();
                        next2 = walker.PeekProcedure(1);
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
                            childrenOrder.Add(Engine.CreatePackNode(Engine.CreateCallFunctionNode("sort", comparatorNode)));
                        }

                        return Engine.CreateNode(dot, NodeType.Prepend, TokenType.Unknown, childrenOrder.ToArray());
                }

            return base.AssembleSuffixNode(node, walker);
        }

        private NodeWalker MoveToStartProcedure(ref NodeWalker walker)
        {
            walker.MoveTo(n => !n.Is(AssignSigns) && !n.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol));
            walker.Move(-1);
            return walker;
        }
        public virtual NodeWalker TrimSeparators(NodeWalker walker)
        {
            return walker?.Current?.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol) == true ? walker.MoveToProcedure() : walker;
        }
    }
}
