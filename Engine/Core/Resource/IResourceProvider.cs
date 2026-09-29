using System.Collections.Generic;

namespace MiMFa.Engine.Resource
{
    public interface IResourceProvider
    {
        bool Exists(string path);
        bool IsAbsolute(string path);
        string Normalize(string path);
        string Resolve(string path, string basePath = null);
        string Combine(params string[] paths);
        string DirectoryName(string path);
        string FileName(string path);
        string Extension(string path);
        IEnumerable<IResourceEntry> GetFolderContents(string path, bool recursive = false);
        string GetFileContents(string path, System.Text.Encoding encoding);
        void SetFileContents(string path, string content);
        void Delete(string path);
    }
}
