using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>
    /// Connects to a Chromium instance started with
    /// Default --remote-debugging-port=9222. It creates a new tab in that instance.
    /// </summary>
    public sealed class BrowserProvider
    {
        private readonly HttpClient _http = new HttpClient();

        public string Platform { get; set; } = "chrome";
        public string Host { get; set; } = "http://127.0.0.1";
        public int Port { get; set; } = 9222;
        public string Endpoint => Host + ":" + Port;

        public BrowserProvider(string host = "http://127.0.0.1", int port = 9222, string platform = "chrome")
        {
            Host = host;
            Port = port;
            Platform = platform;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/version")
                    .ConfigureAwait(false))
                    return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<BrowserPage> LoadAsync(string url, int timeoutMilliseconds = 60000)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("A navigation URL is required.", nameof(url));

            // /json/new is a DevTools endpoint and requires PUT on current Chromium builds.
            string createUrl = Endpoint.TrimEnd('/') + "/json/new?about:blank";
            using (var response = await _http.PutAsync(createUrl, null).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                JObject target = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                string id = (string)target["id"];
                string ws = (string)target["webSocketDebuggerUrl"];
                if (string.IsNullOrWhiteSpace(id) || !Uri.TryCreate(ws, UriKind.Absolute, out Uri wsUri))
                    throw new InvalidOperationException($"The {Platform} returned an invalid page target.");

                var connection = new CdpConnection();
                try
                {
                    await connection.ConnectAsync(wsUri, CancellationToken.None).ConfigureAwait(false);
                    var page = new BrowserPage(id, connection);
                    await page.InitializeAsync().ConfigureAwait(false);
                    await page.NavigateAsync(url, timeoutMilliseconds).ConfigureAwait(false);
                    return page;
                }
                catch
                {
                    connection.Dispose();
                    throw;
                }
            }
        }

        public void Initial()
        {
            System.Diagnostics.Process.Start("powershell", $@"""$env:{Platform}"" --remote-debugging-port={Port} --user-data-dir=""$env:LOCALAPPDATA\DEAL\ChromeProfile""");
        }
        public void Final()
        {
            //  Can remove the debugging port
        }
    }
}
