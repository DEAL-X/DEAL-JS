using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.JavaScript
{
    public class Parser : MiMFa.Engine.Parser.Parser
    {
        public virtual List<Node> ExtractKeywordToken(Token token, TokenWalker walker, bool strict = true)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            if (
                (Engine as Engine).GetKeyword(token) != null ||
                (
                    before != null &&
                    (
                        before.Is("new") ||
                        (
                            strict &&
                            before.Is(TokenType.Statement, TokenType.Prefix, TokenType.Middle, TokenType.Symbol) &&
                            !before.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol) &&
                            !before.Is(":")
                        ) ||
                        (
                            !strict &&
                            before.Is(TokenType.ConcatenatorSymbol)
                        )
                    )
                ) || (
                    next != null &&
                    (
                        (
                            strict &&
                            next.Is(TokenType.Suffix, TokenType.Middle, TokenType.Symbol) &&
                            !next.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol)
                        ) ||
                        (
                            !strict && 
                            next.Is(TokenType.ConcatenatorSymbol)
                        )
                    )
                )
            )
            {
                return ParseKeywordToken(token.Update(TokenType.Keyword), walker).ToList();
            }
            return new List<Node>();
        }

        protected override IEnumerable<Node> ParseStatementToken(Token token, TokenWalker walker)
        {
            var keywords = ExtractKeywordToken(token, walker, false);
            if (keywords.Count > 0)
            {
                foreach (var item in keywords)
                    yield return item;
                yield break;
            }
            switch (token.Value)
            {
                case "get":
                case "set":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend);
                    yield break;
                case "function":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    yield break;

                case "void":
                    yield return new Node(token, NodeType.Prepend | NodeType.Line);
                    yield break;

                case "await":
                    yield return new Node(token, NodeType.CallStructure | NodeType.Prepend);
                    yield break;

                case "async":
                case "typeof":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend);
                    yield break;

                case "new":
                    yield return new Node(token, NodeType.CallStructure | NodeType.Prepend);
                    yield break;

                case "if":
                    yield return new Node(token, NodeType.ConditionStructure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;
                case "else":
                    yield return new Node(token, NodeType.ConditionStructure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;

                case "switch":
                    yield return new Node(token, NodeType.ConditionStructure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;
                case "case":
                    yield return new Node(token, NodeType.CallStructure | NodeType.Prepend);
                    yield break;
                case "default":
                    yield return new Node(token, NodeType.CallStructure | NodeType.Chunk);
                    yield break;

                case "for":
                    yield return new Node(token, NodeType.IterationStructure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;

                case "while":
                    yield return new Node(token, NodeType.IterationStructure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;

                case "do":
                    yield return new Node(token, NodeType.IterationStructure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;

                case "break":
                case "continue":
                case "return":
                    yield return new Node(token, NodeType.Prepend);
                    yield break;
                case "yield":
                case "throw":
                    yield return new Node(token, NodeType.Prepend);
                    yield break;

                case "try":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;
                case "catch":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;
                case "finally":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;

                case "var":
                case "let":
                case "const":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend);
                    yield break;

            }

            keywords = ExtractKeywordToken(token, walker);
            if (keywords.Count > 0)
            {
                foreach (var item in keywords)
                    yield return item;
                yield break;
            }
            switch (token.Value)
            {
                case "implements":
                case "extends":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                yield break;

                case "interface":
                case "class":
                case "enum":
                case "package":
                default:
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;

                case "import":
                case "export":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Region | NodeType.Line);
                    yield break;

                case "debugger":
                    yield return new Node(token, NodeType.Prepend);
                    yield break;

                case "delete":
                    yield return new Node(token, NodeType.Prepend);
                    yield break;

                case "private":
                case "protected":
                case "internal":
                case "public":
                case "static":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend);
                    yield break;
            }
        }
        protected override IEnumerable<Node> ParseScopeToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.Start))
                if (token.Is("{")) yield return new Node(token, NodeType.BlockStructure | NodeType.Region | NodeType.Line);
                else yield return new Node(token, NodeType.BlockStructure | NodeType.Line);
            else if (token.Is(TokenType.End))
                if (token.Is("}")) yield return new Node(token, NodeType.BlockStructure | NodeType.Line);
                else yield return new Node(token, NodeType.BlockStructure);
            else yield return new Node(token, NodeType.Chunk);
        }
        protected override IEnumerable<Node> ParseDataToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.StringData))
            {
                if (token.Is(TokenType.PatternData)) yield return new Node(token, NodeType.Chunk);
                else if (token.Is(TokenType.TemplateStringData)) yield return new Node(token.Clone(null, "`" + token.Value + "`", null), NodeType.Chunk);
                else yield return new Node(token.Clone(null, Newtonsoft.Json.JsonConvert.ToString(token.Value), null), NodeType.Chunk);
            }
            else if (token.Is(TokenType.ArrayData)) yield return new Node(token.Clone(null, "[]", null), NodeType.Chunk);
            else if (token.Is(TokenType.ObjectData)) yield return new Node(token.Clone(null, "{}", null), NodeType.Chunk);
            else yield return new Node(token.Clone(null, token.Value.ToLower(), null), NodeType.Chunk);
        }
        protected override IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.DelimiterSymbol)) yield return new Node(token, NodeType.Append);
            else if (token.Is(TokenType.TerminatorSymbol)) yield return new Node(token, NodeType.Append);
            else if (token.Is(TokenType.ConcatenatorSymbol)) yield return new Node(token, NodeType.Depend);
            else if (token.Is(TokenType.Suffix)) yield return new Node(token, NodeType.Append);
            else if (token.Is(":")) yield return new Node(token, NodeType.Prepend | NodeType.Append);
            else if (token.Is("!")) yield return new Node(token, NodeType.Prepend);
            else if (token.Is("=>", "?", "??")) yield return new Node(token, NodeType.Depend);
            else if (token.Is("="))
            {
                (Engine as Engine).SetKeyword(walker.PeekProcedure(-2));
                yield return new Node(token, NodeType.Depend);
            }
            else if (token.Is(TokenType.Start)) yield return new Node(token, NodeType.Prepend);
            else if (token.Is(TokenType.Prefix)) yield return new Node(token, NodeType.Prepend);
            else if (token.Is(TokenType.Middle)) yield return new Node(token, NodeType.Depend);
            else if (token.Is(TokenType.Suffix)) yield return new Node(token, NodeType.Append);
            else if (token.Is(TokenType.End)) yield return new Node(token, NodeType.Append);
            else yield return new Node(token, NodeType.Depend);
        }
        protected override IEnumerable<Node> ParseKeywordToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            if (next != null && token.Type == TokenType.Keyword)
                if (next.Is(TokenType.Start | TokenType.Scope) && next.Is("("))
                    yield return new Node(token.Clone(TokenType.FunctionKeyword), NodeType.CallStructure | NodeType.Prepend);
                else if (next.Is(TokenType.ConcatenatorSymbol))
                    yield return new Node(token.Clone(TokenType.NamespaceKeyword), NodeType.CallStructure | NodeType.Prepend);
                else yield return new Node(token.Clone(TokenType.IdentifierKeyword), NodeType.CallStructure);
            else yield return new Node(token, NodeType.CallStructure);
        }
        protected override IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker)
        {
            string before = walker.Peek(-2)?.Value ?? "";
            yield return new Node(token,
                token.Value.StartsWith("//") ? (
                    before.StartsWith("/*") ||
                    before == "}" ||
                    (!before.StartsWith("//") && walker.Peek()?.Value?.StartsWith("//") == true) ?
                        NodeType.Region | NodeType.Line :
                        NodeType.Append | NodeType.Line
                ) : (
                    token.Value.Contains("\n") ?
                        NodeType.Region | NodeType.Line | NodeType.Prepend :
                        NodeType.Prepend
                )
            );
        }
        protected override IEnumerable<Node> ParseStartToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.Scope))
                if (token.Is("{")) yield return new Node(token, NodeType.BlockStructure | NodeType.Region | NodeType.Line);
                else yield return new Node(token, NodeType.BlockStructure | NodeType.Line);
            else yield return new Node(token, NodeType.Chunk | NodeType.Prepend);
        }
        protected override IEnumerable<Node> ParsePrefixToken(Token token, TokenWalker walker)
        {
            var keywords = ExtractKeywordToken(token, walker);
            if (keywords.Count > 0)
            {
                foreach (var item in keywords)
                    yield return item;
                yield break;
            }
            else yield return new Node(token, NodeType.Prepend);
        }
        protected override IEnumerable<Node> ParseMiddleToken(Token token, TokenWalker walker)
        {
            var keywords = ExtractKeywordToken(token, walker, false);
            if (keywords.Count > 0)
            {
                foreach (var item in keywords)
                    yield return item;
                yield break;
            }
            else yield return new Node(token, NodeType.Depend);
        }
        protected override IEnumerable<Node> ParseSuffixToken(Token token, TokenWalker walker)
        {
            var keywords = ExtractKeywordToken(token, walker);
            if (keywords.Count > 0)
            {
                foreach (var item in keywords)
                    yield return item;
                yield break;
            }
            else yield return new Node(token, NodeType.Append);
        }
        protected override IEnumerable<Node> ParseEndToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.Scope))
                if (token.Is("}")) yield return new Node(token, NodeType.BlockStructure | NodeType.Line);
                else yield return new Node(token, NodeType.BlockStructure);
            else yield return new Node(token, NodeType.Chunk | NodeType.Append);
        }
        protected override IEnumerable<Node> ParseUnknownToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Chunk);
        }
    }
}
