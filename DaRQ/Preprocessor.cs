using MiMFa.Compiler.Model;
using System.Collections.Generic;

namespace MiMFa.Compiler.DaRQ
{
    public class Preprocessor : MiMFa.Compiler.JavaScript.Preprocessor
    {
        // No additional behavior beyond JavaScript preprocessor for now.
        public new Compiler Compiler { get; set; }

        public override bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler as Compiler);
            return true;
        }
    }
}
