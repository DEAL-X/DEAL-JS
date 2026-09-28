using MiMFa.Compiler.Model;
using MiMFa.Compiler.Walker;
using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Compiler.Assembler
{
    public abstract class Assembler : IStage
    {
        public virtual Compiler Compiler { get; set; }

        public virtual bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) this.Compiler = compiler;
            return true;
        }
        public virtual object Transform(object input, MiMFa.Compiler.Compiler compiler)
        {
            var walker = new NodeWalker((Node[])input, compiler?.Input?.Source);
            return new MiMFa.Compiler.Model.Program(compiler?.Input?.Source, Assemble(walker, compiler).ToArray());
        }

        public virtual IEnumerable<Node> Assemble(NodeWalker walker, MiMFa.Compiler.Compiler compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
                yield return AssembleNode(walker);
        }

        protected virtual Node AssembleNode(NodeWalker walker)
        {
            return AssembleNode(walker.Walk(), walker);
        }
        protected virtual Node AssembleNode(Node node, NodeWalker walker)
        {
            Node latest = null;
            if (latest == null && node.Is(TokenType.Statement))
                 latest = AssembleStatementNode(node, walker);
            if (latest == null && node.Is(TokenType.Scope))
                 latest = AssembleScopeNode(node, walker);
            if (latest == null && node.Is(TokenType.Data))
                 latest = AssembleDataNode(node, walker);
            if (latest == null && node.Is(TokenType.Symbol))
                 latest = AssembleSymbolNode(node, walker);
            if (latest == null && node.Is(TokenType.Keyword))
                 latest = AssembleKeywordNode(node, walker);
            if (latest == null && node.Is(TokenType.Comment))
                 latest = AssembleCommentNode(node, walker);
            if (latest == null && node.Is(TokenType.Start))
                 latest = AssembleStartNode(node, walker);
            if (latest == null && node.Is(TokenType.Prefix))
                 latest = AssemblePrefixNode(node, walker);
            if (latest == null && node.Is(TokenType.Middle))
                 latest = AssembleMiddleNode(node, walker);
            if (latest == null && node.Is(TokenType.Suffix))
                 latest = AssembleSuffixNode(node, walker);
            if (latest == null && node.Is(TokenType.End))
                 latest = AssembleEndNode(node, walker);
            if (latest == null && node.Is(TokenType.None))
                 latest = node.Update(NodeType.None);
            if (latest == null) latest = AssembleUnknownNode(node, walker);
            return latest;
        }

        protected abstract Node AssembleStatementNode(Node node, NodeWalker walker);
        protected abstract Node AssembleScopeNode(Node node, NodeWalker walker);
        protected abstract Node AssembleDataNode(Node node, NodeWalker walker);
        protected abstract Node AssembleSymbolNode(Node node, NodeWalker walker);
        protected abstract Node AssembleKeywordNode(Node node, NodeWalker walker);
        protected abstract Node AssembleCommentNode(Node node, NodeWalker walker);
        protected abstract Node AssembleStartNode(Node node, NodeWalker walker);
        protected abstract Node AssemblePrefixNode(Node node, NodeWalker walker);
        protected abstract Node AssembleMiddleNode(Node node, NodeWalker walker);
        protected abstract Node AssembleSuffixNode(Node node, NodeWalker walker);
        protected abstract Node AssembleEndNode(Node node, NodeWalker walker);
        protected abstract Node AssembleUnknownNode(Node node, NodeWalker walker);
    }
}
