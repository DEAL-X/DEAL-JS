using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace MiMFa.Net
{
    /// <summary>Resolves web addresses and local filesystem paths to absolute addresses.</summary>
    public sealed class Router
    {
        public string RootDirectory { get; set; } = Environment.CurrentDirectory;
        public string RootAddress { get; set; }

        public string Resolve(string address, string baseAddress = null)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("An address is required.", nameof(address));
            address = Environment.ExpandEnvironmentVariables(address.Trim());
            if (HasExplicitScheme(address) &&
                Uri.TryCreate(address, UriKind.Absolute, out var absolute) &&
                !LooksLikeWindowsDrivePath(address))
                return NormalizeUri(absolute);

            if (LooksLikeWebAddress(address))
            {
                var webAddress = address.StartsWith("//", StringComparison.Ordinal)
                    ? "http:" + address
                    : "http://" + address;
                if (Uri.TryCreate(webAddress, UriKind.Absolute, out var webUri))
                    return NormalizeUri(webUri);
            }

            var effectiveBaseAddress = string.IsNullOrWhiteSpace(baseAddress) ? RootAddress : baseAddress;
            if (!LooksLikeLocalPath(address) &&
                Uri.TryCreate(address, UriKind.Relative, out var relative) &&
                !string.IsNullOrWhiteSpace(effectiveBaseAddress) &&
                Uri.TryCreate(effectiveBaseAddress, UriKind.Absolute, out var baseUri) &&
                (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps))
                return NormalizeUri(new Uri(baseUri, relative));

            var localPath = address;
            if (localPath.StartsWith("~/", StringComparison.Ordinal) ||
                localPath.StartsWith(@"~\", StringComparison.Ordinal))
                localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), localPath.Substring(2));
            if (!Path.IsPathRooted(localPath))
                localPath = Path.Combine(string.IsNullOrWhiteSpace(RootDirectory) ? Environment.CurrentDirectory : RootDirectory, localPath);
            return new Uri(Path.GetFullPath(localPath)).AbsoluteUri;
        }

        public string ResolveResource(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("A resource address is required.", nameof(address));
            address = Environment.ExpandEnvironmentVariables(address.Trim());
            if (HasExplicitScheme(address) || LooksLikeWebAddress(address))
                return Resolve(address);
            if (address.StartsWith("~/", StringComparison.Ordinal) || address.StartsWith(@"~\", StringComparison.Ordinal))
                address = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), address.Substring(2));
            if (!Path.IsPathRooted(address))
                address = Path.Combine(string.IsNullOrWhiteSpace(RootDirectory) ? Environment.CurrentDirectory : RootDirectory, address);
            return new Uri(Path.GetFullPath(address)).AbsoluteUri;
        }

        public Uri ResolveUri(string address) => new Uri(Resolve(address), UriKind.Absolute);

        public string ResolveLocalPath(string address)
        {
            var uri = ResolveUri(address);
            if (!uri.IsFile) throw new ArgumentException("The address does not resolve to a local file.", nameof(address));
            return uri.LocalPath;
        }

        private static string NormalizeUri(Uri uri)
        {
            if (uri.IsFile) return new Uri(Path.GetFullPath(uri.LocalPath)).AbsoluteUri;
            return uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
        }

        private static bool HasExplicitScheme(string value) =>
            Regex.IsMatch(value, @"^[a-z][a-z0-9+.-]*://", RegexOptions.IgnoreCase) ||
            value.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

        private static bool LooksLikeWindowsDrivePath(string value) =>
            value.Length >= 3 && char.IsLetter(value[0]) && value[1] == ':' &&
            (value[2] == '\\' || value[2] == '/');

        private static bool LooksLikeLocalPath(string value)
        {
            if (value.StartsWith(".", StringComparison.Ordinal) || value.StartsWith("~", StringComparison.Ordinal) ||
                value.StartsWith("/", StringComparison.Ordinal) || value.StartsWith(@"\\", StringComparison.Ordinal) ||
                LooksLikeWindowsDrivePath(value)) return true;
                        string fileName = value.Split('/', '\\').Last();
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return new[] { ".txt", ".md", ".djs", ".js", ".mjs", ".json", ".html", ".htm", ".css", ".xml", ".csv", ".pdf", ".png", ".jpg", ".jpeg", ".gif" }.Contains(extension);
        }

        private static bool LooksLikeWebAddress(string value)
        {
            if (value.StartsWith("//", StringComparison.Ordinal)) return true;
            if (value.StartsWith(".", StringComparison.Ordinal) || value.StartsWith("~", StringComparison.Ordinal) ||
                value.StartsWith("/", StringComparison.Ordinal) || value.StartsWith(@"\", StringComparison.Ordinal) ||
                LooksLikeWindowsDrivePath(value)) return false;
            if (File.Exists(value)) return false;
            string host = value.Split('/', '\\')[0];
            if (host.Contains(":") || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;
            if (host.Contains("."))
            {
                string extension = Path.GetExtension(host).ToLowerInvariant();
                if (new[] { ".txt", ".md", ".djs", ".js", ".mjs", ".json", ".html", ".htm", ".css", ".xml", ".csv", ".pdf", ".png", ".jpg", ".jpeg", ".gif" }.Contains(extension))
                    return false;
                return true;
            }
            return false;
        }
    }
}