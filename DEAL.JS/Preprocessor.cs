using MiMFa.Engine.Model;
using System.Collections.Generic;

namespace MiMFa.Engine.DEAL.JS
{
    public class Preprocessor : MiMFa.Engine.JavaScript.Preprocessor
    {
        // No additional behavior beyond JavaScript preprocessor for now.
        public new Engine Engine { get; set; }

        public override bool Initialize(MiMFa.Engine.Engine engine)
        {
            if (engine != null) base.Initialize(Engine = engine as Engine);
            return true;
        }
    }
}
