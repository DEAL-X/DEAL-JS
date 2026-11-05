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
        private readonly SemaphoreSlim _browserConnectionLock = new SemaphoreSlim(1, 1);
        private CdpConnection _browserConnection;
        private int? _windowId;
        private string _currentTargetId;
        public int? WindowId => _windowId;
        private readonly ConcurrentDictionary<string, BrowserPage> _pages =
            new ConcurrentDictionary<string, BrowserPage>();
        private readonly ConcurrentDictionary<string, BrowserTab> _tabs =
            new ConcurrentDictionary<string, BrowserTab>();
        public PathProvider PathProvider { get; set; }
        public BrowserTabs Tabs { get; }
        public BrowserDownloads Downloads { get; }
        public BrowserPermissions Permissions { get; }
        public BrowserRuntime Runtime { get; }
        public BrowserDownloads downloads => Downloads;
        public BrowserPermissions permissions => Permissions;
        public BrowserRuntime runtime => Runtime;
        public BrowserTab Tab
        {
            get
            {
                if (CurrentPage != null) return GetTab(CurrentPage);
                if (string.IsNullOrEmpty(_currentTargetId))
                    GetTabsAsync().GetAwaiter().GetResult();
                if (string.IsNullOrEmpty(_currentTargetId)) return null;
                var current = GetTabAsync(_currentTargetId).GetAwaiter().GetResult();
                CurrentPage = current.Page;
                return current;
            }
        }
        // Lower-case aliases form the public JavaScript-facing API.
        public BrowserTabs tabs => Tabs;
        public BrowserTab tab => Tab;
        // Browser-level operations. Tab navigation and history belong to BrowserTab.
        public Task<BrowserTab> open(string url = null) => OpenTabAsync(url ?? "about:blank");
        public void close() => CloseBrowser();
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

        public BrowserProvider(string host = null, int? port = null, string driverPath = null, int? windowId = null)
        {
            Host = host ?? "http://127.0.0.1";
            Port = port ?? 0;
            DriverPath = driverPath;
            _windowId = windowId;
            Tabs = new BrowserTabs(this);
            Downloads = new BrowserDownloads(this);
            Permissions = new BrowserPermissions(this);
            Runtime = new BrowserRuntime(this);
            RandomizedPort = !port.HasValue;
        }

        public void Dispose()
        {
            foreach (var item in _pages.Values)
                item.Dispose();
            _pages.Clear();
            _tabs.Clear();
            CurrentPage = null;
            _currentTargetId = null;
            _browserConnection?.Dispose();
            _browserConnection = null;

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
            _browserConnectionLock.Dispose();
        }

        public void Initial()
        {
            if (IsAlive) return;
            if (Port > 0 && IsAvailableAsync().GetAwaiter().GetResult()) return;

            if (string.IsNullOrWhiteSpace(DriverPath))
            {
                string x86Path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe");
                string x64Path = Path.Combine(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)), "Program Files", "Google", "Chrome", "Application", "chrome.exe");
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

        /// <summary>Closes the Chromium browser process while keeping this provider reusable.</summary>
        public void CloseBrowser()
        {
            foreach (var page in _pages.Values) page.Dispose();
            _pages.Clear();
            _tabs.Clear();
            CurrentPage = null;
            _currentTargetId = null;
            _browserConnection?.Dispose();
            _browserConnection = null;
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
                finally { Process.Dispose(); Process = null; }
            }
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

        private async Task EnsureBrowserConnectionAsync()
        {
            if (_browserConnection != null) return;
            await _browserConnectionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_browserConnection != null) return;
                using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/version").ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    JObject version = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    string websocket = (string)version["webSocketDebuggerUrl"];
                    if (!Uri.TryCreate(websocket, UriKind.Absolute, out Uri uri))
                        throw new InvalidOperationException("Chromium did not provide a browser-level DevTools WebSocket URL.");
                    var connection = new CdpConnection();
                    try
                    {
                        await connection.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
                        _browserConnection = connection;
                    }
                    catch { connection.Dispose(); throw; }
                }
            }
            finally { _browserConnectionLock.Release(); }
        }

        internal async Task<JObject> SendBrowserCommandAsync(string method, JObject parameters = null)
        {
            await EnsureAvailableAsync(Timeout).ConfigureAwait(false);
            await EnsureBrowserConnectionAsync().ConfigureAwait(false);
            return await _browserConnection.SendAsync(method, parameters).ConfigureAwait(false);
        }

        private async Task<int> GetWindowIdAsync(string targetId)
        {
            await EnsureBrowserConnectionAsync().ConfigureAwait(false);
            JObject result = await _browserConnection.SendAsync("Browser.getWindowForTarget",
                new JObject { ["targetId"] = targetId }).ConfigureAwait(false);
            return result["windowId"].Value<int>();
        }

        public async Task<IList<BrowserTabInfo>> GetTabsAsync()
        {
            await EnsureAvailableAsync(Timeout).ConfigureAwait(false);
            using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/list").ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var items = JArray.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var pages = items.OfType<JObject>()
                    .Where(x => string.Equals((string)x["type"], "page", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var result = new List<BrowserTabInfo>();
                foreach (var item in pages)
                {
                    string id = (string)item["id"];
                    int windowId = await GetWindowIdAsync(id).ConfigureAwait(false);
                    if (!_windowId.HasValue) _windowId = windowId;
                    if (_windowId.Value != windowId) continue;
                    result.Add(new BrowserTabInfo
                    {
                        Id = id,
                        Type = (string)item["type"],
                        Title = (string)item["title"],
                        Url = (string)item["url"],
                        WebSocketDebuggerUrl = (string)item["webSocketDebuggerUrl"],
                        IsCurrent = !string.IsNullOrEmpty(_currentTargetId) && _currentTargetId == id
                    });
                }
                if (string.IsNullOrEmpty(_currentTargetId) && result.Count > 0)
                    _currentTargetId = result[0].Id;
                foreach (var item in result) item.IsCurrent = item.Id == _currentTargetId;
                var activeIds = new HashSet<string>(result.Select(x => x.Id));
                foreach (var id in _pages.Keys.Where(x => !activeIds.Contains(x)).ToList())
                {
                    if (_pages.TryRemove(id, out var stale)) stale.Dispose();
                    _tabs.TryRemove(id, out _);
                    if (CurrentPage != null && CurrentPage.TargetId == id) CurrentPage = null;
                    if (_currentTargetId == id) _currentTargetId = result.FirstOrDefault()?.Id;
                }
                return result;
            }
        }

        public async Task<BrowserPage> GetPageAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) throw new ArgumentException("A browser tab identifier is required.", nameof(targetId));
            if (_pages.TryGetValue(targetId, out var existing))
            {
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

        public async Task<string> GoAsync(string address, int timeoutMilliseconds = 0)
        {
            if (string.IsNullOrWhiteSpace(address)) return await GoBackAsync().ConfigureAwait(false);
            string url = PathProvider == null ? address : PathProvider.Resolve(address, CurrentPage?.Url);
            if (CurrentPage == null)
            {
                var page = await LoadAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : Timeout).ConfigureAwait(false);
                return page.Url;
            }
            return await CurrentPage.NavigateAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : Timeout).ConfigureAwait(false);
        }

        public async Task<string> ReloadAsync()
        {
            if (CurrentPage == null) return null;
            return await CurrentPage.ReloadAsync().ConfigureAwait(false);
        }

        public async Task<string> SetLocationAsync(string address, bool addHistoryEntry)
        {
            if (CurrentPage == null) throw new InvalidOperationException("There is no current browser tab.");
            string url = PathProvider == null ? address : PathProvider.Resolve(address, CurrentPage.Url);
            string operation = addHistoryEntry ? "pushState" : "replaceState";
            return await CurrentPage.EvaluateAsync("history." + operation + "(null, null, " + Newtonsoft.Json.JsonConvert.ToString(url) + "); location.href").ConfigureAwait(false);
        }

        public Task<string> GoBackAsync() => CurrentPage == null ? Task.FromResult<string>(null) : CurrentPage.GoBackAsync();
        public Task<string> GoForwardAsync() => CurrentPage == null ? Task.FromResult<string>(null) : CurrentPage.GoForwardAsync();

        public BrowserPage Load(string url) => LoadAsync(url).GetAwaiter().GetResult();

        public Task<BrowserPage> LoadAsync(string url) => LoadAsync(url, Timeout);

        public async Task<BrowserPage> LoadAsync(string url, int timeoutMilliseconds = -1)
        {
            if (string.IsNullOrWhiteSpace(url)) url = "about:blank";
            else if (PathProvider != null) url = PathProvider.Resolve(url);
            if (timeoutMilliseconds <= 0) timeoutMilliseconds = Timeout;
            await EnsureAvailableAsync(timeoutMilliseconds).ConfigureAwait(false);
            await EnsureBrowserConnectionAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(_currentTargetId))
                await GetTabsAsync().ConfigureAwait(false);

            // Activate an anchor tab first so Chromium creates the new tab in this
            // BrowserProvider's application-level window, not another window.
            if (!string.IsNullOrEmpty(_currentTargetId))
                await _browserConnection.SendAsync("Target.activateTarget",
                    new JObject { ["targetId"] = _currentTargetId }).ConfigureAwait(false);

            JObject created = await _browserConnection.SendAsync("Target.createTarget",
                new JObject { ["url"] = "about:blank", ["newWindow"] = false, ["background"] = false })
                .ConfigureAwait(false);
            string id = (string)created["targetId"];
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Chromium did not return the identifier of the new tab.");

            try
            {
                int createdWindowId = await GetWindowIdAsync(id).ConfigureAwait(false);
                if (!_windowId.HasValue) _windowId = createdWindowId;
                if (_windowId.Value != createdWindowId)
                    throw new InvalidOperationException("Chromium created the new tab in a different application-level window.");

                BrowserTabInfo info = null;
                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
                while (info == null && DateTime.UtcNow < deadline)
                {
                    using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/list").ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var targets = JArray.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                        var item = targets.OfType<JObject>().FirstOrDefault(x => (string)x["id"] == id);
                        if (item != null)
                            info = new BrowserTabInfo
                            {
                                Id = id,
                                Type = (string)item["type"],
                                Title = (string)item["title"],
                                Url = (string)item["url"],
                                WebSocketDebuggerUrl = (string)item["webSocketDebuggerUrl"]
                            };
                    }
                    if (info == null) await Task.Delay(50).ConfigureAwait(false);
                }
                if (info == null || !Uri.TryCreate(info.WebSocketDebuggerUrl, UriKind.Absolute, out Uri wsUri))
                    throw new TimeoutException("The new tab did not become available through the DevTools endpoint.");

                var connection = new CdpConnection();
                try
                {
                    await connection.ConnectAsync(wsUri, CancellationToken.None).ConfigureAwait(false);
                    var page = new BrowserPage(id, connection);
                    await page.InitializeAsync().ConfigureAwait(false);
                    await page.NavigateAsync(url, timeoutMilliseconds).ConfigureAwait(false);
                    _pages[id] = page;
                    CurrentPage = page;
                    _currentTargetId = id;
                    return page;
                }
                catch
                {
                    connection.Dispose();
                    throw;
                }
            }
            catch
            {
                try { await _browserConnection.SendAsync("Target.closeTarget", new JObject { ["targetId"] = id }).ConfigureAwait(false); }
                catch { }
                throw;
            }
        }

        public async Task<bool> ActivateAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            await EnsureAvailableAsync(Timeout).ConfigureAwait(false);
            var tabs = await GetTabsAsync().ConfigureAwait(false);
            if (!tabs.Any(x => x.Id == targetId)) return false;
            await EnsureBrowserConnectionAsync().ConfigureAwait(false);
            JObject result = await _browserConnection.SendAsync("Target.activateTarget",
                new JObject { ["targetId"] = targetId }).ConfigureAwait(false);
            if (result["success"] != null && !result["success"].Value<bool>()) return false;
            var page = await GetPageAsync(targetId).ConfigureAwait(false);
            CurrentPage = page;
            _currentTargetId = targetId;
            return true;
        }

        public async Task<bool> CloseAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            await EnsureAvailableAsync(Timeout).ConfigureAwait(false);
            var tabs = await GetTabsAsync().ConfigureAwait(false);
            if (!tabs.Any(x => x.Id == targetId)) return false;
            await EnsureBrowserConnectionAsync().ConfigureAwait(false);
            JObject result = await _browserConnection.SendAsync("Target.closeTarget",
                new JObject { ["targetId"] = targetId }).ConfigureAwait(false);
            bool success = result["success"] == null || result["success"].Value<bool>();
            if (!success) return false;
            if (_pages.TryRemove(targetId, out var page)) page.Dispose();
            _tabs.TryRemove(targetId, out _);
            if (CurrentPage != null && CurrentPage.TargetId == targetId) CurrentPage = null;
            if (_currentTargetId == targetId)
            {
                _currentTargetId = null;
                var remaining = await GetTabsAsync().ConfigureAwait(false);
                _currentTargetId = remaining.FirstOrDefault()?.Id;
            }
            return true;
        }
    }
}