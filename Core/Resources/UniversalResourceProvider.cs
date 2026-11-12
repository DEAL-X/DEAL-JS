using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

namespace MiMFa.Core.Resources
{
    /// <summary>Resolves web addresses and local filesystem paths to absolute addresses.</summary>
    public sealed class UniversalResourceProvider : ResourceProvider
    {
        public string RootAddress { get; set; }

        public override string Normalize(string path, string basePath = null)
        {
            if (string.IsNullOrEmpty(path)) return path;

            if (IsExplicitPath(path) && Uri.TryCreate(path, UriKind.Absolute, out var absolute))
                return NormalizeUri(absolute);

            if (!IsLocalPath(path))
            {
                if (IsWebPath(path))
                {
                    var webAddress =
                        path.StartsWith("//", StringComparison.Ordinal) ? "http:" + path :
                        (string.IsNullOrEmpty(basePath) ? "http://" : basePath) + path;
                    if (Uri.TryCreate(webAddress, UriKind.Absolute, out var webUri))
                        return NormalizeUri(webUri);
                }

                var effectiveBaseAddress = string.IsNullOrWhiteSpace(basePath) ? RootAddress : path;
                if (Uri.TryCreate(path, UriKind.Relative, out var relative) &&
                    !string.IsNullOrWhiteSpace(effectiveBaseAddress) &&
                    Uri.TryCreate(effectiveBaseAddress, UriKind.Absolute, out var baseUri) &&
                    (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps))
                    return NormalizeUri(new Uri(baseUri, relative));
            }
            return base.Normalize(path);
        }
        private string NormalizeUri(Uri uri)
        {
            if (uri.IsFile) return new Uri(Path.GetFullPath(uri.LocalPath)).AbsoluteUri;
            return uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped);
        }

        private static bool IsExplicitPath(string value) => Regex.IsMatch(value, @"^(file|data|about|mailto):", RegexOptions.IgnoreCase);
        private static bool IsLocalPath(string value) => (value.Length >= 3 && Regex.IsMatch(value, @"(^[A-Z]\:)|(^file:)|(^[\w\-\+\s()]+[/\\]|((\.\w+)?$))", RegexOptions.IgnoreCase)) || Regex.IsMatch(value, @"^[\.\~\/\\]", RegexOptions.IgnoreCase);
        private static bool IsWebPath(string value) => value.Length >= 3 && !Regex.IsMatch(value, @"[\r\n\t\f]") && Regex.IsMatch(value, @"(^[a-z][a-z0-9+.-]+\://)|(^\w+\.\w+\.\w+)|[$#@!~^`\?\&\|%\*]", RegexOptions.IgnoreCase);
    }
}