using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.DEAL.JS
{
    /// <summary>JavaScript token-to-independent-node parser.</summary>
    public class Parser : MiMFa.Engine.JavaScript.Parser
    {
        public new Engine Engine { get; set; }

        public override bool Initialize(MiMFa.Engine.Engine engine)
        {
            if (engine != null) base.Initialize(Engine = engine as Engine);
            return true;
        }

        protected override IEnumerable<Node> ParseStatementToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            switch (token.Value.ToLower())
            {
                case "#":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    yield break;

                case "command":
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    yield break;

                case "do":
                case "begin":
                case "doing":
                    if (token.IsMatch("do") && next?.IsMatch("{") == true) break;
                    yield return new Node(token, NodeType.Structure | NodeType.Region | NodeType.Line);
                    yield break;
                case "end":
                    yield return new Node(token, NodeType.Structure | NodeType.Line);
                    yield break;
            }

            foreach (var node in base.ParseStatementToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            switch (token.Value.ToLower())
            {
                case "be":
                case "is":
                    if (next?.IsMatch("not") == true)
                    {
                        if (next2?.IsMatch("equal", "equals", "be", "===") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!=="), walker);
                        }
                        else if (next2?.Is(TokenType.StringData) == true && next2?.IsMatch("", "empty") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" != \"\""), walker);
                        }
                        else if (next?.IsMatch("==", "=") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!="), walker);
                        }
                        else { 
                            walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!="), walker);
                        }
                    }
                    else if (next?.IsMatch("equal", "equals", "be", "===") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "==="), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.IsMatch("", "empty") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" == \"\"", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "=="), walker);

                case "equal":
                case "equals":
                    if (Engine.IsDelimiters(next))
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "==="), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "==="), walker);

                case "not":
                    if (next?.IsMatch("equal", "equals", "===") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!=="), walker);
                    }
                    else if (next?.IsMatch("be", "==", "=") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!="), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.IsMatch("", "empty") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" != \"\""), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!"), walker);
                default:
                    break;
            }

            if (
                (Engine.IsAcceptors(before) || Engine.IsDelimiters(before) || Engine.IsInitializers(before) || Engine.IsOrganizers(before)) &&
                !Engine.IsSeparators(token) &&
                (Engine.IsSeparators(next) || Engine.IsComplementors(next) || Engine.IsMediators(next) || Engine.IsFinalizers(next) || Engine.IsOrganizers(next))
            )
                return ParseDataToken(token.Clone(TokenType.StringData), walker);
            else if (Engine.IsSeparators(token))
            {
                if (Engine.IsOrganizers(next))
                    return new Node[] { new Node(token.Update(TokenType.TerminatorSymbol, ";"), NodeType.Chunk) };
                else if (token.Is(TokenType.DelimiterSymbol))
                    return new Node[] { new Node(token, NodeType.Depend) };
                else if (token.Is(TokenType.TerminatorSymbol))
                    return new Node[] { new Node(token, NodeType.Prepend) };
            }
            return base.ParseSymbolToken(token, walker);
        }
        protected override IEnumerable<Node> ParseStartToken(Token token, TokenWalker walker)
        {
            switch (token.Value.ToLower())
            {
                case "will":
                    yield return new Node(token, NodeType.Prepend | NodeType.Line);
                    yield break;
            }

            foreach (var node in base.ParseStartToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseMiddleToken(Token token, TokenWalker walker)
        {
            switch (token.Value.ToLower())
            {
                case "as":
                    yield return new Node(token, NodeType.Depend | NodeType.Line);
                    yield break;
            }

            foreach (var node in base.ParseMiddleToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseSuffixToken(Token token, TokenWalker walker)
        {
            var before = walker.PeekProcedure(-2);
            bool dot = before == null || before.Value != ".";

            switch (token.Value.ToLower())
            {
                case "then":
                case "otherwise":
                case "anyway":
                case "where":
                case "distinct":
                case "limit":
                case "order":
                case "keys":
                case "values":
                    yield return new Node(token, NodeType.CallStructure | NodeType.Line);
                    yield break;

                default:
                    break;
            }

            foreach (var node in base.ParseSuffixToken(token, walker))
                yield return node;
        }

    }
}