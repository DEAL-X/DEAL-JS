using MiMFa.Engine.Model;
using MiMFa.Engine.Resource;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MiMFa.Engine.JavaScript
{
    public class Engine : MiMFa.Engine.Engine
    {
        public Dictionary<string, Token> Keywords { get; } = new Dictionary<string, Token>();

        public Engine(StageBase[] compileStages = null, StageBase executeStage = null, Options options = null, ResourceProvider resourceProvider = null)
            : base(compileStages ?? new StageBase[] {
                    new Tokenizer(),
                    new Preprocessor(),
                    new Parser(),
                    new Assembler(),
                    new Generator()
                }, executeStage, options ?? new Options(), resourceProvider??new ResourceProvider())
        {
        }

        public virtual Node SetKeyword(string name, Node node)
        {
            if (name == null) return null;
            Keywords[name] = node.Token;
            return node;
        }
        public virtual Token SetKeyword(Token token)
        {
            if (token == null) return null;
            Keywords[token.Value] = token;
            return token;
        }
        public virtual string GetKeywordName(string name)
        {
            if (Keywords.ContainsKey(name)) return name;
            return null;
        }
        public virtual Token GetKeyword(string name)
        {
            if(Keywords.ContainsKey(name)) return Keywords[name];
            return null;
        }

        public virtual bool IsIndependent(Node node) => !node.Is(NodeType.BlockStructure, NodeType.CallStructure, NodeType.DefineStructure) && node.Is(NodeType.Program, NodeType.Structure, NodeType.Independ);
        public virtual bool IsDependent(Node node) => node.Is(NodeType.Append, NodeType.Prepend, NodeType.Depend, NodeType.Chunk, NodeType.BlockStructure, NodeType.CallStructure, NodeType.DefineStructure);
        public virtual bool IsAppendent(Node node) => node.Is(NodeType.Append, NodeType.Chunk, NodeType.BlockStructure, NodeType.CallStructure);
        public virtual bool IsPrependent(Node node) => !node.Is(NodeType.BlockStructure) && node.Is(NodeType.Prepend, NodeType.Chunk, NodeType.CallStructure, NodeType.DefineStructure);

        public virtual bool IsFlag(Node node) => IsFlag(node?.Token);
        public virtual bool IsFlag(Token token) => token != null && token.Is(TokenType.End | TokenType.Scope);

        public virtual bool IsInitializers(Node node) => IsInitializers(node?.Token);
        public virtual bool IsInitializers(Token token) => token != null && token.Is(TokenType.Start, TokenType.Prefix);
        public virtual bool IsConnectors(Node node) => node != null && (IsConnectors(node.Token) || (false && node.Is(TokenType.Comment) && node.Is(NodeType.Prepend)));
        public virtual bool IsConnectors(Token token) => token != null && (token.IsMatch("[", "(") || token.Is(TokenType.Middle | TokenType.Symbol, TokenType.Suffix | TokenType.Symbol));
        public virtual bool IsComplementors(Node node) => IsComplementors(node?.Token);
        public virtual bool IsComplementors(Token token) => token != null && token.Is(TokenType.Suffix, TokenType.ConcatenatorSymbol);
        public virtual bool IsMediators(Node node) => IsMediators(node?.Token);
        public virtual bool IsMediators(Token token) => token != null && token.Is(TokenType.Middle);
        public virtual bool IsSeparators(Node node) => IsSeparators(node?.Token);
        public virtual bool IsSeparators(Token token) => token != null && token.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol);
        public virtual bool IsDelimiters(Node node) => IsDelimiters(node?.Token);
        public virtual bool IsDelimiters(Token token) => token != null && token.Is(TokenType.DelimiterSymbol);
        public virtual bool IsFinalizers(Node node) => IsFinalizers(node?.Token);
        public virtual bool IsFinalizers(Token token) => token != null && token.Is(TokenType.End, TokenType.TerminatorSymbol);
        public virtual bool IsOrganizers(Node node) => IsOrganizers(node?.Token);
        public virtual bool IsOrganizers(Token token) => token != null && token.Is(TokenType.Statement, TokenType.TerminatorSymbol);

    }
}
