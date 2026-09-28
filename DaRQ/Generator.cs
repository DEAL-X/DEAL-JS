using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Compiler.DaRQ
{
    public class Generator : MiMFa.Compiler.JavaScript.Generator
    {
        public new Compiler Compiler { get; set; }

        public override bool Initialize(MiMFa.Compiler.Compiler compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler as Compiler);
            return true;
        }
    }
}
