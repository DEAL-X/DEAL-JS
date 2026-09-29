using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine.Core;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.Tokenizer
{
    public abstract class Tokenizer : StageBase
    {
        public override object Transform(object input, MiMFa.Engine.Engine compiler)
        {
            var walker = new CodeWalker((string)input, compiler?.Input?.Source);
            return Tokenize(walker, compiler).ToArray();
        }
        public virtual IEnumerable<Token> Tokenize(CodeWalker walker, MiMFa.Engine.Engine compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
                yield return TokenizeCode(walker);
        }

        protected abstract Token TokenizeCode(CodeWalker walker);
    }
}
