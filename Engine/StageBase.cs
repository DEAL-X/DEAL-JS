namespace MiMFa.Engine
{
    public abstract class StageBase
    {
        public virtual Engine Compiler { get; set; }

        public virtual bool Initialize(Engine compiler)
        {
            if (compiler != null) Compiler = compiler;
            return true;
        }

        public virtual object Transform(object input, Engine compiler)
        {
            Initialize(compiler);
            return input;
        }
    }
}
