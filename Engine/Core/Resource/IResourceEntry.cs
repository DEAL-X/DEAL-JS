namespace MiMFa.Engine.Resource
{
    public interface IResourceEntry
    {
        string Name { get; }
        string Path { get; }
        bool IsFile { get; }
        bool IsFolder { get; }
    }
}
