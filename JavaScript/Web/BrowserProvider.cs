using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
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

        public void Dispose()
        {
            CurrentPage?.Dispose();
            CurrentPage = null;
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

            if (string.IsNullOrWhiteSpace(DriverPath))
            {
                string x86Path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Google", "Chrome", "Application", "chrome.exe"
                );

                string x64Path = Path.Combine(
                    Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)),
                    "Program Files", "Google", "Chrome", "Application", "chrome.exe"
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

            if(RandomizedPort || Port <= 0) Port = (new Random()).Next(9000,99999);

            var startInfo = new ProcessStartInfo
            {
                FileName = DriverPath,
                Arguments = $"--remote-debugging-port={Port} --user-data-dir=\"{profilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            };

            Process = Process.Start(startInfo);

            if (Process == null)
                throw new InvalidOperationException("Failed to start the browser.");
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


        public BrowserPage Load(string url)
        {
            var r = LoadAsync(url);
            r.Wait();
            return r.Result;
        }

        public async Task<BrowserPage> LoadAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) url = "about:blank";

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
                    await page.NavigateAsync(url, Timeout).ConfigureAwait(false);
                    CurrentPage?.Dispose();
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
