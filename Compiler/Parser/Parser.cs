using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Compiler.Core;
using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;

namespace MiMFa.Compiler.Parser
{
    /// <summary>
    /// Lexical parser stage. It consumes exactly one token per iteration and
    /// produces exactly one independent node for that token. Hierarchy and
    /// language semantics belong to the assembler stage.
    /// </summary>
    public abstract class Parser : IStage
    {
        public virtual Compiler Compiler { get; set; }

        public virtual bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) Compiler = compiler;
            return true;
        }

        public virtual object Transform(object input, Compiler compiler)
        {
            var walker = new TokenWalker((Token[])input, compiler?.Input?.Source);
            return Parse(walker, compiler).ToArray();
        }

        public virtual IEnumerable<Node> Parse(TokenWalker walker, Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
                foreach (var item in ParseTokens(walker.Walk(), walker))
                    yield return item;
        }

        protected virtual IEnumerable<Node> ParseTokens(Token token, TokenWalker walker)
        {
            Node latest = null;
            if (latest == null && token.Is(TokenType.Statement))
                foreach (var node in ParseStatementToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Scope))
                foreach (var node in ParseScopeToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Data))
                foreach (var node in ParseDataToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Symbol))
                foreach (var node in ParseSymbolToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Keyword))
                foreach (var node in ParseKeywordToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Comment))
                foreach (var node in ParseCommentToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Start))
                foreach (var node in ParseStartToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Prefix))
                foreach (var node in ParsePrefixToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Middle))
                foreach (var node in ParseMiddleToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.Suffix))
                foreach (var node in ParseSuffixToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.End))
                foreach (var node in ParseEndToken(token, walker))
                    yield return latest = node;
            if (latest == null && token.Is(TokenType.None))
                yield return new Node { Token = token, Type = NodeType.None };
            if (latest == null)
                foreach (var node in ParseUnknownToken(token, walker))
                    yield return latest = node;
            if (latest == null) yield return new Node { Token = token, Type = NodeType.Unknown };
        }

        // Kept as extension points for semantic/legacy parsers.
        protected abstract IEnumerable<Node> ParseStatementToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseScopeToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseDataToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseSymbolToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseKeywordToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseCommentToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseStartToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParsePrefixToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseMiddleToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseSuffixToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseEndToken(Token token, TokenWalker walker);
        protected abstract IEnumerable<Node> ParseUnknownToken(Token token, TokenWalker walker);
    }
}
