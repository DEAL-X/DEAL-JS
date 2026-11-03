
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>
    /// Provides the native HTTP transport for the JavaScript Fetch API.
    ///
    /// JavaScript communicates with this class through method arguments.
    /// Binary request and response bodies are transferred using Base64.
    /// </summary>
    public sealed class FetchTransport : IDisposable
    {
        private HttpClient _client;
        private CookieContainer _cookies;

        private readonly ConcurrentDictionary<string, CancellationTokenSource>
            _requests = new ConcurrentDictionary<string, CancellationTokenSource>();

        public Encoding Encoding = Encoding.UTF8;

        public bool IsAlive { get; private set; } = false;

        public FetchTransport()
        {
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
        // Fetch
        // ------------------------------------------------------------

        /// <summary>
        /// Sends an HTTP request.
        ///
        /// If bodyBase64 is supplied, it takes precedence over data.
        /// This allows arbitrary binary request bodies.
        /// </summary>
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
                throw new ObjectDisposedException(nameof(FetchTransport));

            id = id ?? Guid.NewGuid().ToString("N");

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("The request URL is missing.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out var requestUri) ||
                (requestUri.Scheme != Uri.UriSchemeHttp &&
                 requestUri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException(
                    "Only absolute HTTP and HTTPS URLs are supported.");
            }

            method = string.IsNullOrWhiteSpace(method)
                ? "GET"
                : method.ToUpperInvariant();

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

                    // The host has no browser-origin context, so same-origin
                    // and include both use the host cookie jar. omit sends none.
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
                                    try { _cookies.SetCookies(requestUri, setCookie); }
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

        // ------------------------------------------------------------
        // Synchronous methods
        // ------------------------------------------------------------

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
