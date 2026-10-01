using System.Collections.Generic;
using System.Linq;

namespace MiMFa.Engine.DEAL.JS
{
    public class Generator : MiMFa.Engine.JavaScript.Generator
    {
        public new Engine Engine { get; set; }

        public override bool Initialize(MiMFa.Engine.Engine engine)
        {
            if (engine != null) base.Initialize(Engine = engine as Engine);
            return true;
        }
    }
}
