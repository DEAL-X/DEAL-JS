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
using MiMFa.Net.Browsers.Services;
using MiMFa.Net.Browsers.Objects;
using MiMFa.Net.Browsers.Protocols.DevTools;
using MiMFa.Net.Browsers.Drivers;
using MiMFa.Net.Browsers.Sessions;
using MiMFa.Net.Browsers.Protocols.Common;
using MiMFa.Net;

namespace MiMFa.Net.Browsers.Sessions
{
    /// <summary>
    /// Controls Chromium page targets (browser tabs) through the DevTools Protocol.
    /// </summary>
    internal sealed class ChromiumBrowserSession : BrowserSession, IBrowserCommandSession
    {
        private readonly Browser _browser;
        private readonly ChromiumDriver _driver;
        private readonly HttpClient _http = new HttpClient();
        private readonly SemaphoreSlim _browserConnectionLock = new SemaphoreSlim(1, 1);
        private DevToolsConnection _browserConnection;
        private int? _windowId;
        private string _currentTargetId;
        public override int? WindowId => _windowId;
        private readonly ConcurrentDictionary<string, BrowserPage> _pages =
            new ConcurrentDictionary<string, BrowserPage>();
        private readonly ConcurrentDictionary<string, BrowserTab> _tabs =
            new ConcurrentDictionary<string, BrowserTab>();
        public override BrowserTab Tab
        {
            get
            {
                var tabs = GetTabsAsync().GetAwaiter().GetResult();
                var current = tabs.FirstOrDefault(item => item.IsCurrent);
                if (current == null) return null;
                var page = GetPageAsync(current.Id).GetAwaiter().GetResult();
                CurrentPage = page;
                _currentTargetId = current.Id;
                return GetTab(page);
            }
        }
        public override BrowserPage CurrentPage { get; protected set; }

        internal ChromiumBrowserSession(Browser browser, ChromiumDriver driver, int? windowId = null)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _windowId = windowId;
        }



        public override void Dispose()
        {
            foreach (var item in _pages.Values)
                item.Dispose();
            _pages.Clear();
            _tabs.Clear();
            CurrentPage = null;
            _currentTargetId = null;
            _windowId = null;
            _browserConnection?.Dispose();
            _browserConnection = null;

            _http.Dispose();
            _browserConnectionLock.Dispose();
        }

        public override void Initial() => _driver.Initial();

        /// <summary>Closes the Chromium browser process while keeping this provider reusable.</summary>
        public override void CloseBrowser()
        {
            foreach (var page in _pages.Values) page.Dispose();
            _pages.Clear();
            _tabs.Clear();
            CurrentPage = null;
            _currentTargetId = null;
            _windowId = null;
            if (_browserConnection != null)
            {
                try { _browserConnection.SendAsync("Browser.close").GetAwaiter().GetResult(); }
                catch { /* The browser may already be shutting down. */ }
                _browserConnection.Dispose();
                _browserConnection = null;
            }
            _driver.Close();
        }

        public void Final() => Dispose();

        public override bool IsAvailable() => IsAvailableAsync().GetAwaiter().GetResult();

        public override Task<bool> IsAvailableAsync() => _driver.IsAvailableAsync();

        private async Task EnsureAvailableAsync(int timeoutMilliseconds)
        {
            if (!_driver.IsAlive) Initial();
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds > 0 ? timeoutMilliseconds : _driver.Timeout);
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
                using (var response = await _http.GetAsync(_driver.Endpoint.TrimEnd('/') + "/json/version").ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    JObject version = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    string websocket = (string)version["webSocketDebuggerUrl"];
                    if (!Uri.TryCreate(websocket, UriKind.Absolute, out Uri uri))
                        throw new InvalidOperationException("Chromium did not provide a browser-level DevTools WebSocket URL.");
                    var connection = new DevToolsConnection();
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

        public JObject SendBrowserCommand(string method, JObject parameters = null) => SendBrowserCommandAsync(method, parameters).GetAwaiter().GetResult();

        public async Task<JObject> SendBrowserCommandAsync(string method, JObject parameters = null)
        {
            await EnsureAvailableAsync(_driver.Timeout).ConfigureAwait(false);
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

        private async Task<string> GetVisibleTabIdAsync(IList<BrowserTab> tabs)
        {
            // Check the previously current tab first. This is the common path and
            // avoids probing every other tab on each browser.Tab property access.
            var previous = tabs.FirstOrDefault(item => item.Id == _currentTargetId);
            if (previous != null)
            {
                try
                {
                    var page = await GetPageAsync(previous).ConfigureAwait(false);
                    var visibility = await page.EvaluateValueAsync("document.visibilityState").ConfigureAwait(false);
                    if (string.Equals(Convert.ToString(visibility), "visible", StringComparison.OrdinalIgnoreCase))
                        return previous.Id;
                }
                catch
                {
                    // A target can disappear while the tab list is being inspected.
                }
            }

            foreach (var tab in tabs)
            {
                if (tab.Id == _currentTargetId) continue;
                try
                {
                    var page = await GetPageAsync(tab).ConfigureAwait(false);
                    var visibility = await page.EvaluateValueAsync("document.visibilityState").ConfigureAwait(false);
                    if (string.Equals(Convert.ToString(visibility), "visible", StringComparison.OrdinalIgnoreCase))
                        return tab.Id;
                }
                catch
                {
                    // A target can disappear while the tab list is being inspected.
                }
            }
            return null;
        }

        public override BrowserTabs GetTabs() => GetTabsAsync().GetAwaiter().GetResult();

        public override async Task<BrowserTabs> GetTabsAsync()
        {
            await EnsureAvailableAsync(_driver.Timeout).ConfigureAwait(false);
            using (var response = await _http.GetAsync(_driver.Endpoint.TrimEnd('/') + "/json/list").ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                var items = JArray.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var pages = items.OfType<JObject>()
                    .Where(x => string.Equals((string)x["type"], "page", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var result = new List<BrowserTab>();
                foreach (var item in pages)
                {
                    string id = (string)item["id"];
                    int windowId = await GetWindowIdAsync(id).ConfigureAwait(false);
                    if (!_windowId.HasValue) _windowId = windowId;
                    if (_windowId.Value != windowId) continue;
                    var tab = new BrowserTab(_browser, id, (string)item["type"], (string)item["webSocketDebuggerUrl"],
                        !string.IsNullOrEmpty(_currentTargetId) && _currentTargetId == id);
                    if (_tabs.TryGetValue(id, out var existingTab))
                    {
                        existingTab.Type = tab.Type;
                        existingTab.WebSocketDebuggerUrl = tab.WebSocketDebuggerUrl;
                        existingTab.IsCurrent = tab.IsCurrent;
                        tab = existingTab;
                    }
                    else
                        tab = _tabs.GetOrAdd(id, tab);
                    result.Add(tab);
                }
                string visibleTargetId = await GetVisibleTabIdAsync(result).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(visibleTargetId))
                    _currentTargetId = visibleTargetId;
                else if (!result.Any(item => item.Id == _currentTargetId))
                    _currentTargetId = null;
                foreach (var item in result) item.IsCurrent = item.Id == _currentTargetId;
                var currentTab = result.FirstOrDefault(item => item.Id == _currentTargetId);
                CurrentPage = currentTab == null ? null : await GetPageAsync(currentTab).ConfigureAwait(false);
                var activeIds = new HashSet<string>(result.Select(x => x.Id));
                foreach (var id in _pages.Keys.Concat(_tabs.Keys).Where(x => !activeIds.Contains(x)).Distinct().ToList())
                {
                    if (_pages.TryRemove(id, out var stale)) stale.Dispose();
                    _tabs.TryRemove(id, out _);
                    if (CurrentPage != null && CurrentPage.TargetId == id) CurrentPage = null;
                    if (_currentTargetId == id) _currentTargetId = null;
                }
                return new BrowserTabs(_browser, result);
            }
        }

        private async Task<BrowserPage> GetPageAsync(BrowserTab tab)
        {
            if (tab == null) throw new ArgumentNullException(nameof(tab));
            if (_pages.TryGetValue(tab.Id, out var existing)) return existing;
            if (!Uri.TryCreate(tab.WebSocketDebuggerUrl, UriKind.Absolute, out Uri wsUri))
                throw new InvalidOperationException("The browser returned an invalid DevTools WebSocket URL.");

            var connection = new DevToolsConnection();
            try
            {
                await connection.ConnectAsync(wsUri, CancellationToken.None).ConfigureAwait(false);
                var page = new BrowserPage(new DevToolsPageSession(tab.Id, connection));
                await page.InitializeAsync().ConfigureAwait(false);
                if (!_pages.TryAdd(tab.Id, page))
                {
                    page.Dispose();
                    page = _pages[tab.Id];
                }
                return page;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        public override BrowserPage GetPage(string targetId) => GetPageAsync(targetId).GetAwaiter().GetResult();

        public override async Task<BrowserPage> GetPageAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("A browser tab identifier is required.", nameof(targetId));
            if (_pages.TryGetValue(targetId, out var existing)) return existing;

            var tabs = await GetTabsAsync().ConfigureAwait(false);
            var tab = tabs.FirstOrDefault(item => item.Id == targetId);
            if (tab == null) throw new KeyNotFoundException("The browser tab was not found: " + targetId);
            return await GetPageAsync(tab).ConfigureAwait(false);
        }

        private BrowserTab GetTab(BrowserPage page)
        {
            var tab = _tabs.GetOrAdd(page.TargetId, _ => new BrowserTab(_browser, page));
            tab.IsCurrent = page.TargetId == _currentTargetId;
            return tab;
        }

        public override BrowserTab GetTab(string targetId) => GetTabAsync(targetId).GetAwaiter().GetResult();

        public override async Task<BrowserTab> GetTabAsync(string targetId)
        {
            var page = await GetPageAsync(targetId).ConfigureAwait(false);
            return GetTab(page);
        }

        public override BrowserTab OpenTab(string url, int timeoutMilliseconds = 0) => OpenTabAsync(url, timeoutMilliseconds).GetAwaiter().GetResult();

        public override async Task<BrowserTab> OpenTabAsync(string url, int timeoutMilliseconds = 0)
        {
            var page = await OpenAsync(url, timeoutMilliseconds).ConfigureAwait(false);
            return GetTab(page);
        }

        public override BrowserPage OpenPage(string url, int timeoutMilliseconds = 0) => OpenAsync(url, timeoutMilliseconds).GetAwaiter().GetResult();

        public override async Task<BrowserPage> OpenAsync(string url, int timeoutMilliseconds = 0)
        {
            return await LoadAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : _driver.Timeout).ConfigureAwait(false);
        }

        public override string Go(string address, int timeoutMilliseconds = 0) => GoAsync(address, timeoutMilliseconds).GetAwaiter().GetResult();

        public override async Task<string> GoAsync(string address, int timeoutMilliseconds = 0)
        {
            if (string.IsNullOrWhiteSpace(address)) return await GoBackAsync().ConfigureAwait(false);
            string url = _browser.Router == null ? address : _browser.Router.Resolve(address, CurrentPage?.Url);
            if (CurrentPage == null)
            {
                var page = await LoadAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : _driver.Timeout).ConfigureAwait(false);
                return page.Url;
            }
            return await CurrentPage.NavigateAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : _driver.Timeout).ConfigureAwait(false);
        }

        public override string Reload() => ReloadAsync().GetAwaiter().GetResult();

        public override async Task<string> ReloadAsync()
        {
            if (CurrentPage == null) return null;
            return await CurrentPage.ReloadAsync().ConfigureAwait(false);
        }

        public override string SetLocation(string address, bool addHistoryEntry) => SetLocationAsync(address, addHistoryEntry).GetAwaiter().GetResult();

        public override async Task<string> SetLocationAsync(string address, bool addHistoryEntry)
        {
            if (CurrentPage == null) throw new InvalidOperationException("There is no current browser tab.");
            string url = _browser.Router == null ? address : _browser.Router.Resolve(address, CurrentPage.Url);
            string operation = addHistoryEntry ? "pushState" : "replaceState";
            return await CurrentPage.EvaluateAsync("history." + operation + "(null, null, " + Newtonsoft.Json.JsonConvert.ToString(url) + "); location.href").ConfigureAwait(false);
        }

        public override string GoBack() => GoBackAsync().GetAwaiter().GetResult();
        public override Task<string> GoBackAsync() => CurrentPage == null ? Task.FromResult<string>(null) : CurrentPage.GoBackAsync();
        public override string GoForward() => GoForwardAsync().GetAwaiter().GetResult();
        public override Task<string> GoForwardAsync() => CurrentPage == null ? Task.FromResult<string>(null) : CurrentPage.GoForwardAsync();

        public override BrowserPage Load(string url, int timeoutMilliseconds = -1) => LoadAsync(url, timeoutMilliseconds).GetAwaiter().GetResult();

        public override async Task<BrowserPage> LoadAsync(string url, int timeoutMilliseconds = -1)
        {
            if (string.IsNullOrWhiteSpace(url)) url = "about:blank";
            else if (_browser.Router != null) url = _browser.Router.Resolve(url);
            if (timeoutMilliseconds <= 0) timeoutMilliseconds = _driver.Timeout;
            await EnsureAvailableAsync(timeoutMilliseconds).ConfigureAwait(false);
            await EnsureBrowserConnectionAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(_currentTargetId))
                await GetTabsAsync().ConfigureAwait(false);

            // Activate an anchor tab first so Chromium creates the new tab in this
            // Browser's application-level window, not another window.
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

                BrowserTab info = null;
                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
                while (info == null && DateTime.UtcNow < deadline)
                {
                    using (var response = await _http.GetAsync(_driver.Endpoint.TrimEnd('/') + "/json/list").ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        var targets = JArray.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                        var item = targets.OfType<JObject>().FirstOrDefault(x => (string)x["id"] == id);
                        if (item != null)
                            info = new BrowserTab(_browser, id, (string)item["type"], (string)item["webSocketDebuggerUrl"]);
                    }
                    if (info == null) await Task.Delay(50).ConfigureAwait(false);
                }
                if (info == null || !Uri.TryCreate(info.WebSocketDebuggerUrl, UriKind.Absolute, out Uri wsUri))
                    throw new TimeoutException("The new tab did not become available through the DevTools endpoint.");

                var connection = new DevToolsConnection();
                try
                {
                    await connection.ConnectAsync(wsUri, CancellationToken.None).ConfigureAwait(false);
                    var page = new BrowserPage(new DevToolsPageSession(id, connection));
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

        public override bool Activate(string targetId) => ActivateAsync(targetId).GetAwaiter().GetResult();

        public override async Task<bool> ActivateAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            await EnsureAvailableAsync(_driver.Timeout).ConfigureAwait(false);
            var tabs = await GetTabsAsync().ConfigureAwait(false);
            if (!tabs.Any(x => x.Id == targetId)) return false;
            await EnsureBrowserConnectionAsync().ConfigureAwait(false);
            JObject result = await _browserConnection.SendAsync("Target.activateTarget",
                new JObject { ["targetId"] = targetId }).ConfigureAwait(false);
            if (result["success"] != null && !result["success"].Value<bool>()) return false;
            var page = await GetPageAsync(targetId).ConfigureAwait(false);
            CurrentPage = page;
            _currentTargetId = targetId;
            foreach (var tab in _tabs.Values) tab.IsCurrent = tab.Id == targetId;
            return true;
        }

        public override bool Close(string targetId) => CloseAsync(targetId).GetAwaiter().GetResult();

        public override async Task<bool> CloseAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            await EnsureAvailableAsync(_driver.Timeout).ConfigureAwait(false);
            var tabs = await GetTabsAsync().ConfigureAwait(false);
            int closingIndex = tabs.Select((tab, index) => new { tab, index }).Where(x => x.tab.Id == targetId).Select(x => x.index).DefaultIfEmpty(-1).First();
            if (closingIndex < 0) return false;
            bool wasCurrent = _currentTargetId == targetId || CurrentPage?.TargetId == targetId;
            await EnsureBrowserConnectionAsync().ConfigureAwait(false);

            // Keep the browser window alive when its last tab is closed.
            string replacementId = null;
            if (wasCurrent && tabs.Count == 1)
            {
                var replacement = await OpenTabAsync("about:blank").ConfigureAwait(false);
                replacementId = replacement.Id;
            }

            JObject result = await _browserConnection.SendAsync("Target.closeTarget",
                new JObject { ["targetId"] = targetId }).ConfigureAwait(false);
            bool success = result["success"] == null || result["success"].Value<bool>();
            if (!success) return false;
            if (_pages.TryRemove(targetId, out var page)) page.Dispose();
            _tabs.TryRemove(targetId, out _);
            if (CurrentPage != null && CurrentPage.TargetId == targetId) CurrentPage = null;

            if (wasCurrent)
            {
                // Wait until Chromium has removed the closed target before choosing
                // the tab that will become the provider's new current (visible) tab.
                var remaining = await GetTabsAsync().ConfigureAwait(false);
                var closeDeadline = DateTime.UtcNow.AddMilliseconds(_driver.Timeout > 0 ? _driver.Timeout : 60000);
                while (remaining.Any(item => item.Id == targetId) && DateTime.UtcNow < closeDeadline)
                {
                    await Task.Delay(50).ConfigureAwait(false);
                    remaining = await GetTabsAsync().ConfigureAwait(false);
                }
                remaining = new BrowserTabs(_browser, remaining.Where(item => item.Id != targetId));

                if (string.IsNullOrEmpty(replacementId))
                    replacementId = remaining.FirstOrDefault(item => item.IsCurrent)?.Id;

                if (string.IsNullOrEmpty(replacementId))
                {
                    // Only fall back to tab-strip order when Chromium reports no
                    // visible page (for example, while the browser window is minimized).
                    replacementId = remaining.ElementAtOrDefault(Math.Min(closingIndex, remaining.Count - 1))?.Id
                        ?? remaining.LastOrDefault()?.Id;
                    if (!string.IsNullOrEmpty(replacementId))
                        await _browserConnection.SendAsync("Target.activateTarget",
                            new JObject { ["targetId"] = replacementId }).ConfigureAwait(false);
                }

                if (!string.IsNullOrEmpty(replacementId))
                {
                    CurrentPage = await GetPageAsync(replacementId).ConfigureAwait(false);
                    _currentTargetId = replacementId;
                    var refreshed = await GetTabsAsync().ConfigureAwait(false);
                    var visible = refreshed.FirstOrDefault(item => item.IsCurrent);
                    if (visible != null && visible.Id != replacementId)
                    {
                        CurrentPage = await GetPageAsync(visible.Id).ConfigureAwait(false);
                        _currentTargetId = visible.Id;
                    }
                }
                else
                {
                    CurrentPage = null;
                    _currentTargetId = null;
                }
            }
            return true;
        }
    }
}