using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public sealed class BrowserTabInfo
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string WebSocketDebuggerUrl { get; set; }
        public bool IsCurrent { get; set; }
    }

    /// <summary>
    /// Controls Chromium page targets (browser tabs) through the DevTools Protocol.
    /// </summary>
    public sealed class BrowserProvider : IDisposable
    {
        private readonly HttpClient _http = new HttpClient();
        private readonly ConcurrentDictionary<string, BrowserPage> _pages =
            new ConcurrentDictionary<string, BrowserPage>();
        private readonly ConcurrentDictionary<string, BrowserTab> _tabs =
            new ConcurrentDictionary<string, BrowserTab>();
        public BrowserTabs Tabs { get; }
        public BrowserTab Tab => CurrentPage == null ? null : GetTab(CurrentPage);
        // Lower-case aliases form the public JavaScript-facing API.
        public BrowserTabs tabs => Tabs;
        public BrowserTab tab => Tab;
        public Task<BrowserTab> open(string url) => OpenTabAsync(url);
        public Task<bool> close(BrowserTab tab) => tab == null ? Task.FromResult(false) : CloseAsync(tab.Id);
        public Task<bool> activate(BrowserTab tab) => tab == null ? Task.FromResult(false) : ActivateAsync(tab.Id);

        public Process Process;
        public string DriverPath { get; set; }
        public string Host { get; set; } = "http://127.0.0.1";
        public int Port { get; set; }
        public bool RandomizedPort { get; set; } = false;
        public string Endpoint => Host.TrimEnd('/') + ":" + Port;
        public int Timeout { get; set; } = 60000;
        public BrowserPage CurrentPage { get; private set; }
        public bool IsAlive => Process != null && !Process.HasExited;

        public BrowserProvider(string host = null, int? port = null, string driverPath = null)
        {
            Host = host ?? "http://127.0.0.1";
            Port = port ?? 0;
            DriverPath = driverPath;
            Tabs = new BrowserTabs(this);
            RandomizedPort = !port.HasValue;
        }

        public void Dispose()
        {
            foreach (var item in _pages.Values)
                item.Dispose();
            _pages.Clear();
            _tabs.Clear();
            CurrentPage = null;

            if (Process != null)
            {
                try
                {
                    if (!Process.HasExited)
                    {
                        Process.CloseMainWindow();
                        if (!Process.WaitForExit(3000)) Process.Kill();
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
            if (IsAlive) return;
            if (Port > 0 && IsAvailableAsync().GetAwaiter().GetResult()) return;

            if (string.IsNullOrWhiteSpace(DriverPath))
            {
                string x86Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
                string x64Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe");
                string localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe");

                if (File.Exists(x86Path)) DriverPath = x86Path;
                else if (File.Exists(x64Path)) DriverPath = x64Path;
                else if (File.Exists(localPath)) DriverPath = localPath;
                else throw new FileNotFoundException("Browser executable was not found.", DriverPath);
            }

            string profilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Drivers", Path.GetFileNameWithoutExtension(DriverPath), "Profile");
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
            if (Process == null) throw new InvalidOperationException("Failed to start the browser.");
        }

        public void Final() => Dispose();

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
                using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/version").ConfigureAwait(false))
                    return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        private async Task EnsureAvailableAsync(int timeoutMilliseconds)
        {
            if (!IsAlive) Initial();
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds > 0 ? timeoutMilliseconds : Timeout);
            while (!await IsAvailableAsync().ConfigureAwait(false))
            {
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException("The Chromium DevTools endpoint did not become available.");
                await Task.Delay(100).ConfigureAwait(false);
            }
        }

        public async Task<IList<BrowserTabInfo>> GetTabsAsync()
        {
            await EnsureAvailableAsync(Timeout).ConfigureAwait(false);
            using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/list").ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var items = JArray.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var result = items.OfType<JObject>()
                    .Where(x => string.Equals((string)x["type"], "page", StringComparison.OrdinalIgnoreCase))
                    .Select(x => new BrowserTabInfo
                    {
                        Id = (string)x["id"],
                        Type = (string)x["type"],
                        Title = (string)x["title"],
                        Url = (string)x["url"],
                        WebSocketDebuggerUrl = (string)x["webSocketDebuggerUrl"],
                        IsCurrent = CurrentPage != null && CurrentPage.TargetId == (string)x["id"]
                    }).ToList();
                var activeIds = new HashSet<string>(result.Select(x => x.Id));
                foreach (var id in _pages.Keys.Where(x => !activeIds.Contains(x)).ToList())
                {
                    if (_pages.TryRemove(id, out var stale)) stale.Dispose();
                    _tabs.TryRemove(id, out _);
                    if (CurrentPage != null && CurrentPage.TargetId == id) CurrentPage = null;
                }
                return result;
            }
        }

        public async Task<BrowserPage> GetPageAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("A browser tab identifier is required.", nameof(targetId));
            if (_pages.TryGetValue(targetId, out var existing))
            {
                CurrentPage = existing;
                return existing;
            }

            var tabs = await GetTabsAsync().ConfigureAwait(false);
            var tab = tabs.FirstOrDefault(x => x.Id == targetId);
            if (tab == null) throw new KeyNotFoundException("The browser tab was not found: " + targetId);
            if (!Uri.TryCreate(tab.WebSocketDebuggerUrl, UriKind.Absolute, out Uri wsUri))
                throw new InvalidOperationException("The browser returned an invalid DevTools WebSocket URL.");

            var connection = new CdpConnection();
            try
            {
                await connection.ConnectAsync(wsUri, CancellationToken.None).ConfigureAwait(false);
                var page = new BrowserPage(tab.Id, connection);
                await page.InitializeAsync().ConfigureAwait(false);
                if (!_pages.TryAdd(targetId, page))
                {
                    page.Dispose();
                    page = _pages[targetId];
                }
                CurrentPage = page;
                return page;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        private BrowserTab GetTab(BrowserPage page)
        {
            return _tabs.GetOrAdd(page.TargetId, _ => new BrowserTab(this, page));
        }

        public async Task<BrowserTab> GetTabAsync(string targetId)
        {
            var page = await GetPageAsync(targetId).ConfigureAwait(false);
            return GetTab(page);
        }

        public async Task<BrowserTab> OpenTabAsync(string url, int timeoutMilliseconds = 0)
        {
            var page = await OpenAsync(url, timeoutMilliseconds).ConfigureAwait(false);
            return GetTab(page);
        }

        public async Task<BrowserPage> OpenAsync(string url, int timeoutMilliseconds = 0)
        {
            return await LoadAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : Timeout).ConfigureAwait(false);
        }

        public BrowserPage Load(string url) => LoadAsync(url).GetAwaiter().GetResult();

        public Task<BrowserPage> LoadAsync(string url) => LoadAsync(url, Timeout);

        public async Task<BrowserPage> LoadAsync(string url, int timeoutMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(url)) url = "about:blank";
            if (timeoutMilliseconds <= 0) timeoutMilliseconds = Timeout;
            await EnsureAvailableAsync(timeoutMilliseconds).ConfigureAwait(false);

            string createUrl = Endpoint.TrimEnd('/') + "/json/new?" + Uri.EscapeDataString(url);
            using (var response = await _http.PutAsync(createUrl, null).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                JObject target = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                string id = (string)target["id"];
                string ws = (string)target["webSocketDebuggerUrl"];
                if (string.IsNullOrWhiteSpace(id) || !Uri.TryCreate(ws, UriKind.Absolute, out Uri wsUri))
                    throw new InvalidOperationException("Browser returned an invalid page target.");

                var connection = new CdpConnection();
                try
                {
                    await connection.ConnectAsync(wsUri, CancellationToken.None).ConfigureAwait(false);
                    var page = new BrowserPage(id, connection);
                    await page.InitializeAsync().ConfigureAwait(false);
                    await page.NavigateAsync(url, timeoutMilliseconds).ConfigureAwait(false);
                    _pages[id] = page;
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

        public async Task<bool> ActivateAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            await EnsureAvailableAsync(Timeout).ConfigureAwait(false);
            using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/activate/" + Uri.EscapeDataString(targetId)).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode) return false;
                if (_pages.TryGetValue(targetId, out var page)) CurrentPage = page;
                else await GetPageAsync(targetId).ConfigureAwait(false);
                return true;
            }
        }

        public async Task<bool> CloseAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            await EnsureAvailableAsync(Timeout).ConfigureAwait(false);
            using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/close/" + Uri.EscapeDataString(targetId)).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode) return false;
                if (_pages.TryRemove(targetId, out var page)) page.Dispose();
                if (CurrentPage != null && CurrentPage.TargetId == targetId) CurrentPage = null;
                return true;
            }
        }
    }
}