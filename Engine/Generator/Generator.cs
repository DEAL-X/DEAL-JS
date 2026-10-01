using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine.Core;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.Generator
{
    public abstract class Generator : StageBase
    {
        protected int Indention = 0;

        public override object Transform(object input, MiMFa.Engine.Engine compiler)
        {
            var program = (Node)input;
            var walker = new NodeWalker(program.Children.ToArray(), program.Token.Value??compiler?.Input?.Source);
            return string.Join(compiler.Options.MakeNewLine(Indention), Generate(walker, compiler).Select(v=>v.Trim()));
        }

        public virtual IEnumerable<string> Generate(NodeWalker walker, MiMFa.Engine.Engine compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
            {
                var c = GenerateCode(walker.Walk(), walker);
                if (c != null) yield return c;
            }
        }

        protected virtual string GenerateCode(Node node, NodeWalker walker)
        {
            string code = null;
            if (node.Is(NodeType.Structure)) code = GenerateStructureCode(node, walker);
            if (code == null && node.Is(NodeType.Program)) code = GenerateProgramCode(node, walker);
            if (code == null && node.Is(NodeType.Region)) code = GenerateRegionCode(node, walker);
            if (code == null && node.Is(NodeType.Line)) code = GenerateLineCode(node, walker);
            if (code == null && node.Is(NodeType.Chunk)) code = GenerateChunkCode(node, walker);
            if (code == null && node.Is(NodeType.Independ)) code = GenerateIndependCode(node, walker);
            if (code == null && node.Is(NodeType.Depend)) code = GenerateDependCode(node, walker);
            if (code == null && node.Is(NodeType.Prepend)) code = GeneratePrependCode(node, walker);
            if (code == null && node.Is(NodeType.Append)) code = GenerateAppendCode(node, walker);
            if (code == null && !node.Is(NodeType.None)) code = GenerateUnknownCode(node, walker);
            return code;
        }

        protected abstract string GenerateStructureCode(Node node, NodeWalker walker);
        protected abstract string GenerateProgramCode(Node node, NodeWalker walker);
        protected abstract string GenerateRegionCode(Node node, NodeWalker walker);
        protected abstract string GenerateLineCode(Node node, NodeWalker walker);
        protected abstract string GenerateChunkCode(Node node, NodeWalker walker);

        protected abstract string GenerateIndependCode(Node node, NodeWalker walker);
        protected abstract string GenerateDependCode(Node node, NodeWalker walker);
        protected abstract string GenerateAppendCode(Node node, NodeWalker walker);
        protected abstract string GeneratePrependCode(Node node, NodeWalker walker);

        protected abstract string GenerateUnknownCode(Node node, NodeWalker walker);
    }
}
