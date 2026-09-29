using MiMFa.Engine.Model;
using System.Collections.Generic;

namespace MiMFa.Engine.DEAL.JS
{
    public class Preprocessor : MiMFa.Engine.JavaScript.Preprocessor
    {
        // No additional behavior beyond JavaScript preprocessor for now.
        public new Engine Compiler { get; set; }

        public override bool Initialize(MiMFa.Engine.Engine compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler as Engine);
            return true;
        }
    }
}
