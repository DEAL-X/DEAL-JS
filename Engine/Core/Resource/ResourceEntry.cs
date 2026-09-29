namespace MiMFa.Engine.Resource
{
    public class ResourceEntry : IResourceEntry
    {
        public string Name { get; }
        public string Path { get; }
        public bool IsFile { get; }
        public bool IsFolder { get; }

        public ResourceEntry(string name, string path, bool isFile, bool isFolder)
        {
            Name = name;
            Path = path;
            IsFile = isFile;
            IsFolder = isFolder;
        }
    }
}
