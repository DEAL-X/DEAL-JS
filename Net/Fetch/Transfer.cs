
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MiMFa.Net;

namespace MiMFa.Net.Fetch
{
    /// <summary>
    /// Provides the native HTTP transport for the JavaScript Fetch API.
    ///
    /// JavaScript communicates with this class through method arguments.
    /// Binary request and response bodies are transferred using Base64.
    /// </summary>
    public sealed class Transfer : IDisposable
    {
        private HttpClient _client;
        private CookieContainer _cookies;

        private readonly ConcurrentDictionary<string, CancellationTokenSource>
            _requests = new ConcurrentDictionary<string, CancellationTokenSource>();

        public Encoding Encoding = Encoding.UTF8;
        public Router Router { get; set; }

        public bool IsAlive { get; private set; } = false;

        public Transfer()
        {
        }

        public void Dispose()
        {
            IsAlive = false;

            foreach (var request in _requests.Values)
            {
                try
                {
                    request.Cancel();
                }
                catch (ObjectDisposedException)
                {
                    // The request completed concurrently.
                }
            }

            _client?.Dispose();
            _client = null;
        }

        // ------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------

        public void Initial()
        {
            if (IsAlive)
                return;

            _cookies = new CookieContainer();

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,

                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate,

                // Cookies are managed per request so credentials:'omit' can
                // be honored by the JavaScript Fetch compatibility layer.
                UseCookies = false
            };

            _client = new HttpClient(handler, true)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };

            IsAlive = true;
        }

        public void Final()
        {
            Dispose();
        }



        // ------------------------------------------------------------
        // Fetch
        // ------------------------------------------------------------

        /// <summary>
        /// Sends an HTTP request.
        ///
        /// If bodyBase64 is supplied, it takes precedence over data.
        /// This allows arbitrary binary request bodies.
        /// </summary>
        public JObject Fetch(FetchRequest request) => FetchAsync(request).GetAwaiter().GetResult();

        public Task<JObject> FetchAsync(FetchRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return FetchAsync(request.Method, request.Url, request.Data, request.ContentType,
                request.Timeout, request.Headers, request.Id, request.BodyBase64, request.Credentials);
        }

        public JObject Fetch(
            string method,
            string url,
            string data = null,
            string contentType = "application/json",
            int timeout = 30000,
            JObject headers = null,
            string id = null,
            string bodyBase64 = null)
        {
            return FetchAsync(
                method,
                url,
                data,
                contentType,
                timeout,
                headers,
                id,
                bodyBase64
            ).GetAwaiter().GetResult();
        }

        public async Task<JObject> FetchAsync(
            string method,
            string url,
            string data = null,
            string contentType = "application/json",
            int timeout = 30000,
            JObject headers = null,
            string id = null,
            string bodyBase64 = null,
            string credentials = "same-origin")
        {
            if (!IsAlive || _client == null)
                throw new ObjectDisposedException(nameof(Transfer));

            id = id ?? Guid.NewGuid().ToString("N");

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("The request URL is missing.");

            method = string.IsNullOrWhiteSpace(method) ? "GET" : method.ToUpperInvariant();
            if ((method == "GET" || method == "HEAD") && (data != null || bodyBase64 != null))
                throw new InvalidOperationException("GET and HEAD requests cannot contain a body.");

            if (Router != null) url = Router.Resolve(url);
            if (!Uri.TryCreate(url, UriKind.Absolute, out var requestUri))
                throw new ArgumentException("The request address could not be resolved to an absolute URL.");
            if (requestUri.IsFile)
                return await ReadLocalFileAsync(method, requestUri, id).ConfigureAwait(false);
            if (requestUri.Scheme != Uri.UriSchemeHttp && requestUri.Scheme != Uri.UriSchemeHttps)
                throw new ArgumentException("Only HTTP, HTTPS, and local file addresses are supported.");

            if (timeout < 0)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be zero (infinite) or a positive number.");

            if (!string.Equals(credentials, "omit", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(credentials, "same-origin", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(credentials, "include", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid Fetch credentials mode.", nameof(credentials));

            if ((method == "GET" || method == "HEAD") &&
                (data != null || bodyBase64 != null))
                throw new InvalidOperationException("GET and HEAD requests cannot contain a body.");

            var cancellation = new CancellationTokenSource();

            if (timeout > 0)
                cancellation.CancelAfter(timeout);

            if (!_requests.TryAdd(id, cancellation))
            {
                cancellation.Dispose();

                throw new InvalidOperationException(
                    "A request with this ID is already active: " + id);
            }

            try
            {
                using (var request = new HttpRequestMessage(
                    new HttpMethod(method),
                    requestUri))
                {
                    // Create the body first. Content headers are applied
                    // afterward so they are not lost when Content is assigned.
                    AddBody(request, data, contentType, bodyBase64);

                    if (headers != null)
                        AddHeaders(request, headers);

                    // Cookie is a forbidden Fetch request header. Ignore any
                    // caller-supplied Cookie value and apply the host cookie jar
                    // only when credentials are not omitted.
                    request.Headers.Remove("Cookie");
                    if (!string.Equals(credentials, "omit", StringComparison.OrdinalIgnoreCase))
                    {
                        string cookieHeader = _cookies.GetCookieHeader(requestUri);
                        if (!string.IsNullOrEmpty(cookieHeader))
                            request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
                    }

                    using (var response = await _client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead,
                        cancellation.Token).ConfigureAwait(false))
                    {
                        if (!string.Equals(credentials, "omit", StringComparison.OrdinalIgnoreCase))
                        {
                            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
                            {
                                foreach (var setCookie in setCookies)
                                {
                                    try { _cookies.SetCookies(response.RequestMessage?.RequestUri ?? requestUri, setCookie); }
                                    catch (CookieException) { /* Ignore malformed server cookie values. */ }
                                }
                            }
                        }

                        var responseBody = response.Content == null
                            ? Array.Empty<byte>()
                            : await response.Content.ReadAsByteArrayAsync()
                                .ConfigureAwait(false);

                        var responseHeaders =
                            new Dictionary<string, string>(
                                StringComparer.OrdinalIgnoreCase);

                        foreach (var header in response.Headers)
                        {
                            responseHeaders[header.Key] =
                                string.Join(", ", header.Value);
                        }

                        if (response.Content != null)
                        {
                            foreach (var header in response.Content.Headers)
                            {
                                responseHeaders[header.Key] =
                                    string.Join(", ", header.Value);
                            }
                        }

                        var finalUrl =
                            response.RequestMessage?.RequestUri?.ToString()
                            ?? url;

                        return JObject.FromObject(new
                        {
                            id = id,
                            type = "basic",
                            url = finalUrl,
                            status = (int)response.StatusCode,
                            statusText = response.ReasonPhrase ?? string.Empty,

                            ok = (int)response.StatusCode >= 200 &&
                                 (int)response.StatusCode <= 299,

                            redirected = !UriEquals(finalUrl, requestUri),

                            // Newtonsoft serializes byte[] as Base64.
                            data = responseBody,

                            headers = responseHeaders
                        });
                    }
                }
            }
            finally
            {
                _requests.TryRemove(id, out _);
                cancellation.Dispose();
            }
        }

        private async Task<JObject> ReadLocalFileAsync(string method, Uri uri, string id)
        {
            if (method != "GET" && method != "HEAD")
                throw new InvalidOperationException("Local file requests support GET and HEAD only.");
            string path = uri.LocalPath;
            if (!File.Exists(path))
                throw new FileNotFoundException("The requested local file was not found.", path);
            byte[] bytes = Array.Empty<byte>();
            if (method != "HEAD")
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
                {
                    if (stream.Length > int.MaxValue)
                        throw new IOException("The requested local file is too large to fit in a byte array.");
                    bytes = new byte[(int)stream.Length];
                    int offset = 0;
                    while (offset < bytes.Length)
                    {
                        int read = await stream.ReadAsync(bytes, offset, bytes.Length - offset).ConfigureAwait(false);
                        if (read <= 0) break;
                        offset += read;
                    }
                    if (offset != bytes.Length) Array.Resize(ref bytes, offset);
                }
            }
            string contentType = GetContentType(path);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Content-Type"] = contentType,
                ["Content-Length"] = new FileInfo(path).Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["Last-Modified"] = File.GetLastWriteTimeUtc(path).ToString("R", System.Globalization.CultureInfo.InvariantCulture)
            };
            return JObject.FromObject(new
            {
                id = id,
                type = "basic",
                url = uri.AbsoluteUri,
                status = 200,
                statusText = "OK",
                ok = true,
                redirected = false,
                data = bytes,
                headers = headers
            });
        }

        private static string GetContentType(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".html": case ".htm": return "text/html; charset=utf-8";
                case ".css": return "text/css; charset=utf-8";
                case ".js": case ".mjs": return "text/javascript; charset=utf-8";
                case ".json": return "application/json; charset=utf-8";
                case ".xml": return "application/xml; charset=utf-8";
                case ".txt": case ".md": case ".djs": return "text/plain; charset=utf-8";
                case ".svg": return "image/svg+xml";
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".pdf": return "application/pdf";
                default: return "application/octet-stream";
            }
        }

        /// <summary>JSON bridge for script hosts that do not expose Newtonsoft types.</summary>
        public JObject FetchJson(string method, string url, string data = null, string contentType = "application/json", int timeout = 30000, string headersJson = null, string id = null, string bodyBase64 = null, string credentials = "same-origin") => FetchJsonAsync(method, url, data, contentType, timeout, headersJson, id, bodyBase64, credentials).GetAwaiter().GetResult();

        public Task<JObject> FetchJsonAsync(
            string method,
            string url,
            string data = null,
            string contentType = "application/json",
            int timeout = 30000,
            string headersJson = null,
            string id = null,
            string bodyBase64 = null,
            string credentials = "same-origin")
        {
            JObject headers = string.IsNullOrWhiteSpace(headersJson) ? null : JObject.Parse(headersJson);
            return FetchAsync(method, url, data, contentType, timeout, headers, id, bodyBase64, credentials);
        }

        // ------------------------------------------------------------
        // Synchronous methods
        // ------------------------------------------------------------



        public JObject Get(
            string url,
            string data = null,
            string contentType = "application/json",
            int timeout = 30000,
            JObject headers = null,
            string id = null,
            string bodyBase64 = null)
        {
            return GetAsync(
                url, data, contentType,
                timeout, headers, id, bodyBase64
            ).GetAwaiter().GetResult();
        }

        public async Task<JObject> GetAsync(
            string url,
            string data = null,
            string contentType = "application/json",
            int timeout = 30000,
            JObject headers = null,
            string id = null,
            string bodyBase64 = null)
        {
            return await FetchAsync(
                "GET", url, data, contentType,
                timeout, headers, id, bodyBase64);
        }



        public JObject Post(
            string url,
            string data = null,
            string contentType = "application/json",
            int timeout = 30000,
            JObject headers = null,
            string id = null,
            string bodyBase64 = null)
        {
            return PostAsync(
                url, data, contentType,
                timeout, headers, id, bodyBase64
            ).GetAwaiter().GetResult();
        }

        public async Task<JObject> PostAsync(
            string url,
            string data = null,
            string contentType = "application/json",
            int timeout = 30000,
            JObject headers = null,
            string id = null,
            string bodyBase64 = null)
        {
            return await FetchAsync(
                "POST", url, data, contentType,
                timeout, headers, id, bodyBase64);
        }



        // ------------------------------------------------------------
        // Cancellation
        // ------------------------------------------------------------

        public bool Abort(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (!_requests.TryGetValue(id, out var cancellation))
                return false;

            try
            {
                cancellation.Cancel();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        // ------------------------------------------------------------
        // Request headers
        // ------------------------------------------------------------

        private void AddHeaders(
            HttpRequestMessage request,
            JObject headers)
        {
            foreach (var header in headers.Properties())
            {
                var value = header.Value.Type == JTokenType.String
                    ? header.Value.Value<string>()
                    : header.Value.ToString(Formatting.None);

                if (string.IsNullOrEmpty(value))
                    continue;

                if (request.Headers.TryAddWithoutValidation(
                    header.Name, value))
                {
                    continue;
                }

                // Content headers require HttpContent.
                if (request.Content == null)
                {
                    request.Content =
                        new ByteArrayContent(Array.Empty<byte>());
                }

                // Replace existing values instead of accidentally
                // appending duplicate Content-Type values.
                request.Content.Headers.Remove(header.Name);

                request.Content.Headers.TryAddWithoutValidation(
                    header.Name, value);
            }
        }

        // ------------------------------------------------------------
        // Request body
        // ------------------------------------------------------------

        private void AddBody(
            HttpRequestMessage request,
            string data,
            string contentType = "application/json",
            string bodyBase64 = null)
        {
            byte[] bytes;

            if (bodyBase64 != null)
            {
                // Binary-safe request body.
                bytes = Convert.FromBase64String(bodyBase64);
            }
            else if (data != null)
            {
                // Text request body.
                bytes = Encoding.GetBytes(data);
            }
            else
            {
                return;
            }

            request.Content = new ByteArrayContent(bytes);

            if (!string.IsNullOrWhiteSpace(contentType))
            {
                request.Content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(contentType);
            }
        }

        // ------------------------------------------------------------
        // Utilities
        // ------------------------------------------------------------

        private bool UriEquals(string first, Uri second)
        {
            if (!Uri.TryCreate(first, UriKind.Absolute, out var firstUri))
                return false;

            return Uri.Compare(
                firstUri,
                second,
                UriComponents.AbsoluteUri,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}
