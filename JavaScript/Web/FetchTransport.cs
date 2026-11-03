using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>
    /// Provides the native HTTP transport for the JavaScript Fetch API.
    ///
    /// JavaScript communicates with this class using JSON descriptors.
    /// Binary request and response bodies are transferred using Base64.
    /// </summary>
    public sealed class FetchTransport : IDisposable
    {
        private readonly HttpClient _client;

        private readonly ConcurrentDictionary<string, CancellationTokenSource>
            _requests = new ConcurrentDictionary<string, CancellationTokenSource>();

        private bool _disposed;

        public FetchTransport()
        {
            var cookies = new CookieContainer();

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,

                AutomaticDecompression =
                    DecompressionMethods.GZip |
                    DecompressionMethods.Deflate,

                UseCookies = true,
                CookieContainer = cookies
            };

            _client = new HttpClient(handler, true)
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

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

            _client.Dispose();
        }

        public void Initial()
        {
        }
        public void Final()
        {
            Dispose();
        }

        /// <summary>
        /// Sends an HTTP request.
        ///
        /// Expected JSON:
        ///
        /// {
        ///   "id": "request-1",
        ///   "url": "https://example.com/",
        ///   "method": "GET",
        ///   "headers": {
        ///     "Accept": "text/html"
        ///   },
        ///   "bodyBase64": "...",
        ///   "contentType": "application/json",
        ///   "timeout": 30000
        /// }
        ///
        /// Returns:
        ///
        /// {
        ///   "id": "request-1",
        ///   "url": "https://example.com/",
        ///   "status": 200,
        ///   "statusText": "OK",
        ///   "ok": true,
        ///   "redirected": false,
        ///   "headers": {},
        ///   "bodyBase64": "..."
        /// }
        /// </summary>
        public async Task<string> SendAsync(string requestJson)
        {
            ThrowIfDisposed();

            if (string.IsNullOrWhiteSpace(requestJson))
                throw new ArgumentException("A request descriptor is required.", nameof(requestJson));

            var root = JObject.Parse(requestJson);

            var id = root.Value<string>("id") ?? Guid.NewGuid().ToString("N");
            var url = root.Value<string>("url");

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("The request URL is missing.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var requestUri) ||
                (requestUri.Scheme != Uri.UriSchemeHttp &&
                 requestUri.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Only absolute HTTP and HTTPS URLs are supported.");

            var method = root.Value<string>("method") ?? "GET";
            var timeout = root.Value<int?>("timeout") ?? 0;

            var cancellation = new CancellationTokenSource();

            if (timeout > 0)
                cancellation.CancelAfter(timeout);

            if (!_requests.TryAdd(id, cancellation))
            {
                cancellation.Dispose();
                throw new InvalidOperationException("A request with this ID is already active: " + id);
            }

            try
            {
                using (var request = new HttpRequestMessage(new HttpMethod(method), requestUri))
                {
                    AddHeaders(request, root);
                    AddBody(request, root);

                    using (var response = await _client.SendAsync(
                        request,
                        HttpCompletionOption.ResponseContentRead,
                        cancellation.Token).ConfigureAwait(false))
                    {
                        var body = response.Content == null
                            ? Array.Empty<byte>()
                            : await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                        var headers = new Dictionary<string, string>(
                            StringComparer.OrdinalIgnoreCase);

                        foreach (var header in response.Headers)
                            headers[header.Key] = string.Join(", ", header.Value);

                        if (response.Content != null)
                        {
                            foreach (var header in response.Content.Headers)
                                headers[header.Key] = string.Join(", ", header.Value);
                        }

                        var finalUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;

                        var result = new
                        {
                            id,
                            type = "basic",
                            url = finalUrl,
                            status = (int)response.StatusCode,
                            statusText = response.ReasonPhrase ?? string.Empty,
                            ok = (int)response.StatusCode >= 200 &&
                                 (int)response.StatusCode <= 299,
                            redirected = !UriEquals(finalUrl, requestUri),
                            headers,
                            bodyBase64 = Convert.ToBase64String(body)
                        };

                        return JsonConvert.SerializeObject(result);
                    }
                }
            }
            finally
            {
                _requests.TryRemove(id, out _);
                cancellation.Dispose();
            }
        }

        /// <summary>
        /// Cancels an active HTTP request.
        /// </summary>
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

        private static void AddHeaders(HttpRequestMessage request, JObject root)
        {
            if (!(root["headers"] is JObject headers))
                return;

            foreach (var header in headers.Properties())
            {
                var value = header.Value.Type == JTokenType.String
                    ? header.Value.Value<string>()
                    : header.Value.ToString(Formatting.None);

                if (string.IsNullOrEmpty(value))
                    continue;

                if (!request.Headers.TryAddWithoutValidation(header.Name, value))
                {
                    if (request.Content == null)
                        request.Content = new ByteArrayContent(Array.Empty<byte>());

                    request.Content.Headers.TryAddWithoutValidation(header.Name, value);
                }
            }
        }

        private static void AddBody(HttpRequestMessage request, JObject root)
        {
            var bodyBase64 = root.Value<string>("bodyBase64");

            if (string.IsNullOrEmpty(bodyBase64))
                return;

            var bytes = Convert.FromBase64String(bodyBase64);

            request.Content = new ByteArrayContent(bytes);

            var contentType = root.Value<string>("contentType");

            if (!string.IsNullOrWhiteSpace(contentType))
                request.Content.Headers.ContentType =
                    MediaTypeHeaderValue.Parse(contentType);
        }

        private static bool UriEquals(string first, Uri second)
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

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FetchTransport));
        }

    }
}