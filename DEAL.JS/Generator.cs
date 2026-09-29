using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Engine.DEAL.JS
{
    public class Generator : MiMFa.Engine.JavaScript.Generator
    {
        public new Engine Compiler { get; set; }

        public override bool Initialize(MiMFa.Engine.Engine compiler)
        {
            if (compiler != null) base.Initialize(Compiler = compiler as Engine);
            return true;
        }
    }
}
