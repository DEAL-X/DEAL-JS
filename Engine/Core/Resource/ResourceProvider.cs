using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MiMFa.Engine.Resource
{
    public class ResourceProvider : IResourceProvider
    {
        public bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

        public bool IsAbsolute(string path) => Path.IsPathRooted(path);

        public string Normalize(string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            var p = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            try { return Path.GetFullPath(p); } catch { return p; }
        }

        public string Resolve(string path, string basePath = null)
        {
            if (IsAbsolute(path)) return Normalize(path);
            if (string.IsNullOrEmpty(basePath)) basePath = Directory.GetCurrentDirectory();
            var combined = Path.Combine(basePath, path);
            return Normalize(combined);
        }

        public string Combine(params string[] paths) => Path.Combine(paths ?? Array.Empty<string>());

        public string DirectoryName(string path) => Path.GetDirectoryName(path);

        public string FileName(string path) => Path.GetFileName(path);

        public string Extension(string path) => Path.GetExtension(path);

        public IEnumerable<IResourceEntry> GetFolderContents(string path, bool recursive = false)
        {
            var results = new List<IResourceEntry>();
            var root = Normalize(path);
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return results;

            try
            {
                foreach (var file in Directory.EnumerateFiles(root))
                    results.Add(new ResourceEntry(Path.GetFileName(file), file, isFile: true, isFolder: false));
                
                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    results.Add(new ResourceEntry(Path.GetFileName(dir), dir, isFile: false, isFolder: true));
                    if (recursive)
                        results.AddRange(GetFolderContents(dir, true));
                }

            }
            catch (UnauthorizedAccessException)
            {
                // Ignore directories we can't access
            }

            return results;
        }

        public string GetFileContents(string path, Encoding encoding)
        {
            var p = Normalize(path);
            if (!File.Exists(p)) return null;
            return File.ReadAllText(p, encoding ?? Encoding.UTF8);
        }

        public void SetFileContents(string path, string content)
        {
            var p = Normalize(path);
            var dir = Path.GetDirectoryName(p);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(p, content ?? string.Empty, Encoding.UTF8);
        }

        public void Delete(string path)
        {
            var p = Normalize(path);
            if (File.Exists(p)) File.Delete(p);
            else if (Directory.Exists(p)) Directory.Delete(p, true);
        }
    }
}
