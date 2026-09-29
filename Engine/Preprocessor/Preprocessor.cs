using System;
using System.Collections.Generic;
using System.Linq;
using MiMFa.Engine.Core;
using MiMFa.Engine.Model;
using MiMFa.Engine.Walker;

namespace MiMFa.Engine.Preprocessor
{
    public abstract class Preprocessor : StageBase
    {
        public override object Transform(object input, MiMFa.Engine.Engine compiler)
        {
            Token[] tokens = input as Token[] ?? (input as IEnumerable<Token>)?.ToArray() ?? new Token[0];
            var walker = new TokenWalker(tokens, compiler?.Input?.Source);
            return Preprocess(walker, compiler).ToArray();
        }
        public virtual IEnumerable<Token> Preprocess(TokenWalker walker, MiMFa.Engine.Engine compiler = null)
        {
            if (!Initialize(compiler)) yield break;
            while (!walker.IsEnded)
                yield return PreprocessToken(walker.Walk(), walker);
        }

        public virtual Token PreprocessToken(Token token, TokenWalker walker) => token;
    }
}
