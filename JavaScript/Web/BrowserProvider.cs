using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Net.Sockets;
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
        private readonly HttpClient _http = new HttpClient();

        public Process Process;
        public string DriverPath { get; set; }
        public string Host { get; set; } = "http://127.0.0.1";
        public int Port { get; set; }
        public bool RandomizedPort { get; set; } = false;
        public string Endpoint => Host + ":" + Port;
        public int Timeout { get; set; } = 60000;
        public BrowserPage CurrentPage { get; private set; }
        public bool IsAlive => Process != null && !Process.HasExited;

        public BrowserProvider(string host = null, int? port = null, string driverPath = null)
        {
            Host = host ?? "http://127.0.0.1";
            Port = port ?? 0;
            DriverPath = driverPath;
            RandomizedPort = !port.HasValue;
        }

        private void CloseCurrentPage()
        {
            if (CurrentPage == null) return;
            try { CurrentPage.Close(); }
            catch { CurrentPage.Dispose(); }
            finally { CurrentPage = null; }
        }

        public void Dispose()
        {
            CloseCurrentPage();
            if (Process != null)
            {
                try
                {
                    if (!Process.HasExited)
                    {
                        Process.CloseMainWindow();

                        if (!Process.WaitForExit(3000))
                            Process.Kill();

                        Process.WaitForExit();
                    }
                }
                finally
                {
                    Process.Dispose();
                    Process = null;
                }
            }

            _http.Dispose();
        }

        public void Initial()
        {
            if (IsAlive)
                return;

            // Reuse an externally started Chromium instance when a port was
            // explicitly configured; do not launch a second browser process.
            if (Port > 0 && IsAvailableAsync().GetAwaiter().GetResult())
                return;

            if (string.IsNullOrWhiteSpace(DriverPath))
            {
                string x86Path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", "chrome.exe"
                );

                string x64Path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Google", "Chrome", "Application", "chrome.exe"
                );

                string localPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Google", "Chrome", "Application", "chrome.exe"
                );

                if (File.Exists(x86Path))
                    DriverPath = x86Path;
                else if(File.Exists(x64Path))
                    DriverPath = x64Path;
                else if (File.Exists(localPath))
                    DriverPath = localPath;
                else
                    throw new FileNotFoundException(
                        "Browser executable was not found.",
                        DriverPath
                    );
            }

            string profilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Drivers",
                Path.GetFileNameWithoutExtension(DriverPath),
                "Profile"
            );

            Directory.CreateDirectory(profilePath);

            if (RandomizedPort || Port <= 0) Port = GetFreePort();

            var startInfo = new ProcessStartInfo
            {
                FileName = DriverPath,
                Arguments = $"--remote-debugging-port={Port} --user-data-dir=\"{profilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            };

            Process = System.Diagnostics.Process.Start(startInfo);

            if (Process == null)
                throw new InvalidOperationException("Failed to start the browser.");
        }
        public void Final()
        {
            Dispose();
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
            finally { listener.Stop(); }
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


        public BrowserPage Load(string url)
        {
            var r = LoadAsync(url);
            r.Wait();
            return r.Result;
        }

        public Task<BrowserPage> LoadAsync(string url)
        {
            return LoadAsync(url, Timeout);
        }

        public async Task<BrowserPage> LoadAsync(string url, int timeoutMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(url)) url = "about:blank";
            if (timeoutMilliseconds <= 0) timeoutMilliseconds = Timeout;

            // Chromium may take a moment to expose its DevTools endpoint after launch.
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
            while (!await IsAvailableAsync().ConfigureAwait(false))
            {
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException("The Chromium DevTools endpoint did not become available.");
                await Task.Delay(100).ConfigureAwait(false);
            }

            // /json/new is a DevTools endpoint and requires PUT on current Chromium builds.
            string createUrl = Endpoint.TrimEnd('/') + "/json/new?" + Uri.EscapeDataString(url);

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
                    CloseCurrentPage();
                    CurrentPage = page;
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
