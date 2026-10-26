using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            List<Node> keywords;
            var next = walker.PeekProcedure();
            switch (token.Value.ToLower())
            {
                case "#":
                case "##":
                case "###":
                case "####":
                case "#####":
                case "######":
                    keywords = ExtractKeywordToken(token, walker);
                    if (keywords.Count > 0)
                    {
                        foreach (var item in keywords)
                            yield return item;
                        yield break;
                    }
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    yield break;

                case "command":
                    keywords = ExtractKeywordToken(token, walker);
                    if (keywords.Count > 0)
                    {
                        foreach (var item in keywords)
                            yield return item;
                        yield break;
                    }
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    yield break;
               
                case "consider":
                    keywords = ExtractKeywordToken(token, walker);
                    if (keywords.Count > 0)
                    {
                        foreach (var item in keywords)
                            yield return item;
                        yield break;
                    }
                    yield return new Node(token, NodeType.DefineStructure | NodeType.Prepend | NodeType.Line);
                    yield break;

                case "do":
                case "begin":
                case "doing":
                    if (token.Is("do") && next?.Is("{") == true) break;
                    keywords = ExtractKeywordToken(token, walker, false);
                    if (keywords.Count > 0)
                    {
                        foreach (var item in keywords)
                            yield return item;
                        yield break;
                    }
                    yield return new Node(token, NodeType.BlockStructure | NodeType.Region | NodeType.Line);
                    yield break;
                case "end":
                    keywords = ExtractKeywordToken(token, walker, false);
                    if (keywords.Count > 0)
                    {
                        foreach (var item in keywords)
                            yield return item;
                        yield break;
                    }
                    yield return new Node(token, NodeType.BlockStructure | NodeType.Line);
                    yield break;
            }

            foreach (var node in base.ParseStatementToken(token, walker))
                yield return node;
        }
        protected override IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker)
        {
            var next = walker.PeekProcedure();
            var next2 = walker.PeekProcedure(1);
            switch (token.Value.ToLower())
            {
                case "be":
                    if (next?.Is("equals", "===") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "==="), walker);
                    }
                    else if (next?.Is("equal", "==", "=") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "=="), walker);
                    }
                    else if (next?.Is("lesser", "less", "<") == true)
                    {
                        if (next2?.Is("than") == true) walker.MoveToProcedure();
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "<"), walker);
                    }
                    else if (next?.Is("greater", "great", ">") == true)
                    {
                        if (next2?.Is("than") == true) walker.MoveToProcedure();
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, ">"), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.Is("", "empty") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Symbol, " = \"\"", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "="), walker);

                case "is":
                    if (next?.Is("not") == true)
                    {
                        if (next2?.Is("equal", "equals", "be", "===") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!=="), walker);
                        }
                        else if (next2?.Is(TokenType.StringData) == true && next2?.Is("", "empty") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" != \"\""), walker);
                        }
                        else if (next?.Is("==", "=") == true)
                        {
                            walker.MoveToProcedure(); walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!="), walker);
                        }
                        else
                        {
                            walker.MoveToProcedure();
                            return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!="), walker);
                        }
                    }
                    else if (next?.Is("equals", "be", "===") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "==="), walker);
                    }
                    else if (next?.Is("equal", "==", "=") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "=="), walker);
                    }
                    else if (next?.Is("lesser", "less", "<") == true)
                    {
                        if (next2?.Is("than") == true) walker.MoveToProcedure();
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "<"), walker);
                    }
                    else if (next?.Is("greater", "great", ">") == true)
                    {
                        if (next2?.Is("than") == true) walker.MoveToProcedure();
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, ">"), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.Is("", "empty") == true)
                    {
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" == \"\"", null), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "=="), walker);

                case "equal":
                    if (Engine.IsDelimiters(next))
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "=="), walker);
                    }
                    else if (next?.Is("lesser", "less", "<") == true)
                    {
                        if (next2?.Is("than") == true) walker.MoveToProcedure();
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "<="), walker);
                    }
                    else if (next?.Is("greater", "great", ">") == true)
                    {
                        if (next2?.Is("than") == true) walker.MoveToProcedure();
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, ">="), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "=="), walker);
                case "equals":
                    if (Engine.IsDelimiters(next))
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "==="), walker);
                    }
                    else if (next?.Is("lesser", "less", "<") == true)
                    {
                        if (next2?.Is("than") == true) walker.MoveToProcedure();
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "<="), walker);
                    }
                    else if (next?.Is("greater", "great", ">") == true)
                    {
                        if (next2?.Is("than") == true) walker.MoveToProcedure();
                        walker.MoveToProcedure(); return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, ">="), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "==="), walker);

                case "not":
                    if (next?.Is("equals", "===") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!=="), walker);
                    }
                    else if(next?.Is("equal", "==", "=") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!="), walker);
                    }
                    else if (next?.Is("be", "==", "=") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!="), walker);
                    }
                    else if (next?.Is(TokenType.StringData) == true && next?.Is("", "empty") == true)
                    {
                        walker.MoveToProcedure();
                        return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "+ \"\" != \"\""), walker);
                    }
                    else return ParseSymbolToken(token.Clone(TokenType.Middle | TokenType.Symbol, "!"), walker);
                default:
                    break;
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
        protected override IEnumerable<Node> ParseSuffixToken(Token token, TokenWalker walker)
        {
            List<Node> keywords;
            switch (token.Value.ToLower())
            {
                case "then":
                case "otherwise":
                case "anyway":
                case "where":
                case "distinct":
                case "limit":
                case "order":
                    keywords = ExtractKeywordToken(token, walker, false);
                    if (keywords.Count > 0)
                    {
                        foreach (var item in keywords)
                            yield return item;
                        yield break;
                    }
                    yield return new Node(token, NodeType.CallStructure | NodeType.Line);
                    yield break;
                case "keys":
                case "values":
                    keywords = ExtractKeywordToken(token, walker);
                    if (keywords.Count > 0)
                    {
                        foreach (var item in keywords)
                            yield return item;
                        yield break;
                    }
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