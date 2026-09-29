using MiMFa.Engine.Model;
using MiMFa.Engine.Resource;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Engine.DEAL.JS
{
    public class Engine : JavaScript.Engine
    {
        public Dictionary<string, string> ActionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> FunctionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> DefinitionCommands { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, string> Reserves { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Engine(StageBase[] compileStages = null, StageBase executeStage = null, Options options = null, ResourceProvider resourceProvider = null)
            : base(compileStages ?? new StageBase[] {
                    new Tokenizer(),
                    new Preprocessor(),
                    new Parser(),
                    new Assembler(),
                    new Generator()
                }, executeStage, options ?? new Options(), resourceProvider ?? new ResourceProvider())
        {
        }

        public virtual Node SetActionCommand(string name, Node node)
        {
            ActionCommands[name.ToLower()] = name + "()";
            return SetKeyword(name, node);
        }
        public virtual Token SetActionCommand(Token token)
        {
            ActionCommands[token.Value.ToLower()] = token.Value + "()";
            return SetKeyword(token);
        }
        public virtual Token GetActionCommand(string name)
        {
            ActionCommands.TryGetValue(name.ToLower(), out string v);
            return GetKeyword(v);
        }
        public virtual string GetActionCommandName(string name)
        {
            ActionCommands.TryGetValue(name.ToLower(), out string label);
            return label;
        }

        public virtual Node SetFunctionCommand(string name, Node node)
        {
            FunctionCommands[name.ToLower()] = name;
            return SetKeyword(name, node);
        }
        public virtual Token SetFunctionCommand(Token token)
        {
            FunctionCommands[token.Value.ToLower()] = token.Value;
            return SetKeyword(token);
        }
        public virtual Token GetFunctionCommand(string name)
        {
            FunctionCommands.TryGetValue(name.ToLower(), out string v);
            return GetKeyword(v);
        }
        public virtual string GetFunctionCommandName(string name)
        {
            FunctionCommands.TryGetValue(name.ToLower(), out string v);
            return v;
        }

        public virtual Node SetDefinitionCommand(string name, Node node)
        {
            DefinitionCommands[name.ToLower()] = name;
            return SetKeyword(name, node);
        }
        public virtual Token SetDefinitionCommand(Token token)
        {
            DefinitionCommands[token.Value.ToLower()] = token.Value;
            return SetKeyword(token);
        }
        public virtual Token GetDefinitionCommand(string name)
        {
            DefinitionCommands.TryGetValue(name.ToLower(), out string v);
            return GetKeyword(v);
        }
        public virtual string GetDefinitionCommandName(string name)
        {
            DefinitionCommands.TryGetValue(name.ToLower(), out string v);
            return v;
        }

        public virtual Token GetCommand(string name)
        {
            return GetFunctionCommand(name) ?? GetDefinitionCommand(name) ?? GetActionCommand(name);
        }
        public virtual string GetCommandName(string name)
        {
            return GetFunctionCommandName(name) ?? GetDefinitionCommandName(name) ?? GetActionCommandName(name);
        }

        public virtual string GetFunctionName(string name)
        {
            if (Keywords.ContainsKey(name) && Keywords[name]?.Is(TokenType.FunctionKeyword) == true)
                return name;
            return null;
        }


        public virtual bool IsGlobalNeeder(Node node) => node != null && node.Has(n => n.Token.IsMatch("return", "yield", "{", "do", "begin", "end"));
        public virtual bool IsAcceptors(Node node) => IsAcceptors(node?.Token);
        public virtual bool IsAcceptors(Token token) => token != null && (token.Is(TokenType.FunctionKeyword) || (GetFunctionName(token.Value) ?? GetFunctionCommandName(token.Value) ?? null) != null);


        public virtual Node TrimSeparators(Node node)
        {
            if (node.Is(NodeType.BlockStructure) && node.Token.IsMatch("{")) return node;
            return node.Trim(n => n.Count <= 0 && n.Ancestor(p => p.Is(NodeType.BlockStructure))?.Token.IsMatch("{") != true && n.Token.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol));
        }

        public virtual Node CreateNode(string value = "", params Node[] children) =>
            CreateNode(value, NodeType.Chunk, TokenType.Unknown, children);
        public virtual Node CreateNode(string value, TokenType tokenType, NodeType nodeType = NodeType.Chunk, params Node[] children) =>
            CreateNode(value, nodeType, tokenType, children);
        public virtual Node CreateNode(string value, NodeType nodeType, TokenType tokenType = TokenType.Unknown, params Node[] children)
        {
            return new Node(new Token(tokenType, value), nodeType, children);
        }
        public virtual Node CreateDotNode(NodeType? type = null)
        {
            return CreateNode(".", TokenType.ConcatenatorSymbol, type ?? NodeType.Depend);
        }
        public virtual Node CreateOpenPackNode(NodeType? type = null)
        {
            return CreateNode("(", TokenType.Start | TokenType.Scope, type?? NodeType.BlockStructure | NodeType.Line | NodeType.Prepend);
        }
        public virtual Node CreateClosePackNode(NodeType? type = null)
        {
            return CreateNode(")", TokenType.End | TokenType.Scope, type ?? NodeType.BlockStructure | NodeType.Append);
        }
        public virtual Node CreateOpenBlockNode(NodeType? type = null)
        {
            return CreateNode("{", TokenType.Start | TokenType.Scope, type ?? NodeType.BlockStructure | NodeType.Region | NodeType.Depend);
        }
        public virtual Node CreateCloseBlockNode(NodeType? type = null)
        {
            return CreateNode("}", TokenType.End | TokenType.Scope, type ?? NodeType.BlockStructure | NodeType.Append);
        }
        public virtual Node CreatePackNode(params Node[] children)
        {
            if (children.Length == 1 && children[0].Is(NodeType.BlockStructure) && children[0].Token.IsMatch("(")) return children[0];
            return new Node(new Token(TokenType.Scope, "("), NodeType.BlockStructure, children);
        }
        public virtual Node CreateLineNode(Node node)
        {
            if (node == null) return CreateNode(";", TokenType.TerminatorSymbol, NodeType.Append);
            node.Type |= NodeType.Line;
            if (node.Seek(n=>n.Parent != null && !n.Is(TokenType.Comment)) == null) return node;
            if (node.LastLeaf.Token.Is(TokenType.DelimiterSymbol, TokenType.TerminatorSymbol))
                node.LastLeaf.Token.Update(TokenType.TerminatorSymbol, ";");
            else if (!node.Is(NodeType.BlockStructure))
                return node.Add(CreateNode(";", TokenType.TerminatorSymbol, NodeType.Append));
            return node.Update(node.Type | NodeType.Line);
        }
        public virtual Node CreateBlockNode(params Node[] children)
        {
            if (children.Length == 1 && children[0].Is(NodeType.BlockStructure) && children[0].Token.IsMatch("{")) return children[0];
            return new Node(new Token(TokenType.Start | TokenType.Scope, "{"), NodeType.BlockStructure | NodeType.Region | NodeType.Line, children);
        }
        public virtual Node CreatePackOrBlockNode(params Node[] children)
        {
            children = children.Where(c => !c.Is(NodeType.None)).ToArray();
            if (children.Length == 0) return CreatePackNode();
            if (children.Length > 1) return CreateBlockNode(children);
            if (children[0].Is(NodeType.BlockStructure)) return children[0];
            if (children[0].Is(NodeType.Structure) || IsGlobalNeeder(children[0])) return CreateBlockNode(children[0]);
            return TrimSeparators(CreatePackNode(children[0]));
        }
        public virtual Node CreateCallNode(Node node, params Node[] args)
        {
            return CreateNode("", TrimSeparators(node), CreatePackNode(args));
        }
        public virtual Node CreateCallFunctionNode(string name, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.CallStructure, CreatePackNode(args));
        }
        public virtual Node CreateDefineIdentifierNode(string name, Node value, string state = "var")
        {
            if (string.IsNullOrEmpty(state)) return new Node(
                new Token(TokenType.IdentifierKeyword, name), NodeType.DefineStructure | NodeType.Line | NodeType.Independ,
                CreateNode("=", TokenType.Middle | TokenType.Symbol, NodeType.Depend, value)
             );
            else return new Node(
                new Token(TokenType.Statement, state), NodeType.DefineStructure | NodeType.Line | NodeType.Independ,
                CreateNode(name, TokenType.IdentifierKeyword, NodeType.Prepend | NodeType.Chunk,
                    CreateNode("=", TokenType.Middle | TokenType.Symbol, NodeType.Depend, value)
                )
            );
        }
        public virtual Node CreateDefineFunctionNode(string name, Node body, params Node[] args)
        {
            return new Node(new Token(TokenType.FunctionKeyword, name), NodeType.DefineStructure,
                CreatePackNode(args),
                body
            );
        }
        public virtual Node CreateProceduresNode(string value = "", params Node[] children)
        {
            return new Node(new Token(TokenType.Unknown, value), NodeType.Prepend, children.ToArray());
        }
        public virtual Node CreateCallableNode(Node body, params Node[] args)
        {
            return CreatePackNode(CreatePackNode(args), CreateNode("=>", TokenType.Middle | TokenType.Symbol, NodeType.Prepend, CreatePackOrBlockNode(body)));
        }
        public virtual IEnumerable<Node> CallableNodes(string args, params Node[] body)
        {
            yield return CreateOpenPackNode();
            if(!string.IsNullOrWhiteSpace(args)) yield return CreateNode(args, TokenType.Keyword, NodeType.Chunk);
            yield return CreateClosePackNode();
            yield return CreateNode("=>", TokenType.Symbol, NodeType.Chunk);
            if (body.First().Is(TokenType.Start | TokenType.Scope))
                foreach (var item in body)
                    yield return item;
            else
            {
                yield return CreateOpenBlockNode();
                foreach (var item in body)
                    yield return item;
                yield return CreateCloseBlockNode();
            }
        }
        public virtual IEnumerable<Node> CallableDataNodes(params Node[] body)
        {
            return CallableNodes("data", body);
        }
        public virtual IEnumerable<Node> CallableHandlerNodes(params Node[] body)
        {
            var data = new Node[] { CreateNode("handlers", TokenType.FunctionKeyword, NodeType.Chunk), CreateOpenPackNode(), CreateNode("data", TokenType.Keyword, NodeType.Chunk), CreateClosePackNode() };
            if (body.First().Is(TokenType.Start | TokenType.Scope))
                body = data.Concat((new Node[] { body.First()}).Concat(body.Skip(1))).ToArray();
            return CallableDataNodes(body);
        }
        public virtual Node CreateNamespaceNode(string ns, Node node)
        {
            return CreateNode(ns + ".", NodeType.Chunk, TokenType.ConcatenatorSymbol, node);
        }
    }
}
