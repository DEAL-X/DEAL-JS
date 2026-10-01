namespace MiMFa.Engine
{
    public abstract class StageBase
    {
        public virtual Engine Engine { get; set; }

        public virtual bool Initialize(Engine engine)
        {
            if (engine != null) Engine = engine;
            return true;
        }

        public virtual object Transform(object input, Engine engine)
        {
            Initialize(engine);
            return input;
        }
    }
}
