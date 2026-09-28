using System.Collections.Generic;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.JavaScript
{
    public class Parser : MiMFa.Compiler.Parser.Parser
    {
        protected override IEnumerable<Node> ParseStatementToken(Token token, TokenWalker walker)
        {
            switch (token.Value)
            {
                case "get":
                case "set":
                case "function":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    break;

                case "implements":
                case "extends":
                    yield return new Node(token, NodeType.CallStructure | NodeType.Depend | NodeType.Line);
                    break;

                case "interface":
                case "class":
                case "enum":
                case "package":
                default:
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Region);
                    break;

                case "if":
                    yield return new Node(token, NodeType.ConditionStructure | NodeType.Prepend | NodeType.Region);
                    break;
                case "else":
                    yield return new Node(token, NodeType.ConditionStructure | NodeType.Depend | NodeType.Region);
                    break;

                case "switch":
                    yield return new Node(token, NodeType.ConditionStructure | NodeType.Prepend | NodeType.Region);
                    break;
                case "case":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Line);
                    break;
                case "default":
                    yield return new Node(token, NodeType.Structure | NodeType.Line);
                    break;

                case "for":
                    yield return new Node(token, NodeType.IterationStructure | NodeType.Prepend | NodeType.Region);
                    break;

                case "while":
                    yield return new Node(token, NodeType.IterationStructure | NodeType.Prepend | NodeType.Region);
                    break;

                case "do":
                    yield return new Node(token, NodeType.IterationStructure | NodeType.Prepend | NodeType.Region);
                    break;

                case "break":
                case "continue":
                    yield return new Node(token, NodeType.Line);
                    break;

                case "return":
                    yield return new Node(token, NodeType.Prepend | NodeType.Line);
                    break;
                case "yield":
                case "throw":
                    yield return new Node(token, NodeType.Prepend | NodeType.Line);
                    break;

                case "try":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Region);
                    break;
                case "catch":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Region);
                    break;
                case "finally":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Region);
                    break;

                case "import":
                case "export":
                    yield return new Node(token, NodeType.Structure | NodeType.Prepend | NodeType.Region);
                    break;

                case "void":
                    yield return new Node(token, NodeType.Prepend | NodeType.Line);
                    break;

                case "var":
                case "let":
                case "const":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    break;

                case "with":
                case "debugger":
                    yield return new Node(token, NodeType.Prepend | NodeType.Line);
                    break;

                case "delete":
                    yield return new Node(token, NodeType.Prepend | NodeType.Line);
                    break;

                case "await":
                    yield return new Node(token, NodeType.CallStructure | NodeType.Prepend | NodeType.Line);
                    break;
                case "async":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    break;
                case "new":
                    yield return new Node(token, NodeType.CallStructure | NodeType.Prepend | NodeType.Line);
                    break;

                case "private":
                case "protected":
                case "internal":
                case "public":
                case "static":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Region);
                    break;
            }
        }
        protected override IEnumerable<Node> ParseScopeToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.Start))
                if (token.IsMatch("{")) yield return new Node(token, NodeType.BlockStructure | NodeType.Region);
                else yield return new Node(token, NodeType.BlockStructure | NodeType.Line);
            else if (token.Is(TokenType.End))
                if (token.IsMatch("}")) yield return new Node(token, NodeType.BlockStructure);
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
            if (token.Is(TokenType.DelimiterSymbol)) yield return new Node(token, NodeType.Depend);
            else if (token.Is(TokenType.TerminatorSymbol, TokenType.Suffix)) yield return new Node(token, NodeType.Append);
            else if (token.Is(TokenType.ConcatenatorSymbol)) yield return new Node(token, NodeType.Depend);
            else if (token.IsMatch(":")) yield return new Node(token, NodeType.Depend);
            else if (token.IsMatch("!")) yield return new Node(token, NodeType.Prepend);
            else if (token.IsMatch("=>", "?", "??")) yield return new Node(token, NodeType.Depend);
            else if (token.IsMatch("="))
            {
                (Compiler as Compiler).SetKeyword(walker.PeekProcedure(-2));
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
            if (next != null)
                if (next.Is(TokenType.Start | TokenType.Scope) && next.IsMatch("("))
                    yield return new Node(token.Clone(TokenType.FunctionKeyword), NodeType.CallStructure | NodeType.Prepend);
                else if (next.Is(TokenType.ConcatenatorSymbol))
                    yield return new Node(token.Clone(TokenType.NamespaceKeyword), NodeType.CallStructure | NodeType.Prepend);
                else yield return new Node(token.Clone(TokenType.IdentifierKeyword), NodeType.CallStructure);
            else yield return new Node(token, NodeType.CallStructure);
        }
        protected override IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, token.Value.StartsWith("//") ? NodeType.Append : (token.Value.Contains("\n") ? NodeType.Region | NodeType.Prepend : NodeType.Prepend));
        }
        protected override IEnumerable<Node> ParseStartToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.Scope))
                if (token.IsMatch("{")) yield return new Node(token, NodeType.BlockStructure | NodeType.Region);
                else yield return new Node(token, NodeType.BlockStructure | NodeType.Line);
            else yield return new Node(token, NodeType.Chunk | NodeType.Prepend);
        }
        protected override IEnumerable<Node> ParsePrefixToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Prepend);
        }
        protected override IEnumerable<Node> ParseMiddleToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Depend);
        }
        protected override IEnumerable<Node> ParseSuffixToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Append);
        }
        protected override IEnumerable<Node> ParseEndToken(Token token, TokenWalker walker)
        {
            if (token.Is(TokenType.Scope))
                if (token.IsMatch("}")) yield return new Node(token, NodeType.BlockStructure);
                else yield return new Node(token, NodeType.BlockStructure);
            else yield return new Node(token, NodeType.Chunk | NodeType.Append);
        }
        protected override IEnumerable<Node> ParseUnknownToken(Token token, TokenWalker walker)
        {
            yield return new Node(token, NodeType.Chunk);
        }
    }
}
