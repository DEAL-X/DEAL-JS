using MiMFa.Engine.Core;
using MiMFa.Engine.Model;
using System;

namespace MiMFa.Engine.Executor
{
    public abstract class Executor : StageBase
    {
        public override object Transform(object input, Engine compiler)
        {
            if (!Initialize(compiler)) return null;
            return input;
        }
    }
}
