using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
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
    public sealed class BrowserProvider : IDisposable
    {
        private Process Process;

        private readonly HttpClient _http = new HttpClient();

        public string DriverPath { get; set; } = @"ProgramFiles\Google\Chrome\Application\chrome.exe";
        public string Host { get; set; } = "http://127.0.0.1";
        public int Port { get; set; } = 9222;
        public string Endpoint => Host + ":" + Port;
        public bool IsDisposed => Process == null || Process.HasExited;

        public BrowserProvider(string host = "http://127.0.0.1", int port = 9222, string driverPath = @"ProgramFiles\Google\Chrome\Application\chrome.exe")
        {
            Host = host;
            Port = port;
            DriverPath = driverPath;
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Process.CloseMainWindow();

                if (!Process.WaitForExit(3000))
                    Process.Kill();
            }
        }

        public void Initial()
        {
            Process = System.Diagnostics.Process.Start("powershell", $@"& ""$env:{DriverPath}"" --remote-debugging-port={Port} --user-data-dir=""$env:LOCALAPPDATA\DEAL\ChromeProfile""");
        }
        public void Final()
        {
            Dispose();
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
                    throw new InvalidOperationException($"Browser returned an invalid page target.");

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

    }
}
