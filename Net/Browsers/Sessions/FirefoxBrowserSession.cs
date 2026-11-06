using MiMFa.Net.Browsers;
using MiMFa.Net.Browsers.Drivers;
using MiMFa.Net.Browsers.Objects;
using MiMFa.Net.Browsers.Protocols.WebDriverBiDi;
using MiMFa.Net.Browsers.Sessions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Sessions
{
    /// <summary>Firefox browser session implemented with WebDriver BiDi browsing contexts.</summary>
    internal sealed class FirefoxBrowserSession : BrowserSession
    {
        private readonly Browser _browser;
        private readonly FirefoxDriver _driver;
        private readonly ConcurrentDictionary<string, BrowserPage> _pages = new ConcurrentDictionary<string, BrowserPage>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, BrowserTab> _tabs = new ConcurrentDictionary<string, BrowserTab>(StringComparer.Ordinal);
        private string _currentContextId;

        public override int? WindowId => null;
        public override BrowserPage CurrentPage { get; protected set; }
        public override BrowserTab Tab
        {
            get
            {
                var tabs = GetTabs();
                var current = tabs.FirstOrDefault(item => item.IsCurrent);
                if (current == null) return null;
                CurrentPage = GetPage(current.Id);
                _currentContextId = current.Id;
                return GetTab(CurrentPage);
            }
        }

        internal FirefoxBrowserSession(Browser browser, FirefoxDriver driver)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        }

        public override void Dispose()
        {
            foreach (var page in _pages.Values) page.Dispose();
            _pages.Clear();
            _tabs.Clear();
            CurrentPage = null;
            _currentContextId = null;
        }

        public override void Initial()
        {
            _driver.Initial();
            _driver.EnsureSession();
            GetTabs();
        }

        public override bool IsAvailable() => _driver.IsAvailable();
        public override Task<bool> IsAvailableAsync() => _driver.IsAvailableAsync();

        public override void CloseBrowser()
        {
            Dispose();
            _driver.Close();
        }



        public override BrowserTabs GetTabs() => GetTabsAsync().GetAwaiter().GetResult();

        public override async Task<BrowserTabs> GetTabsAsync()
        {
            await _driver.EnsureSessionAsync().ConfigureAwait(false);
            JObject tree = await _driver.SendAsync("browsingContext.getTree", new JObject { ["maxDepth"] = 0 }).ConfigureAwait(false);
            var contexts = tree["contexts"] as JArray ?? new JArray();
            var result = new List<BrowserTab>();

            foreach (JObject context in contexts.OfType<JObject>())
            {
                string id = (string)context["context"];
                if (string.IsNullOrWhiteSpace(id)) continue;
                var tab = new BrowserTab(_browser, id);
                if (_tabs.TryGetValue(id, out var existingTab))
                {
                    existingTab.Type = tab.Type;
                    existingTab.IsCurrent = tab.IsCurrent;
                    tab = existingTab;
                }
                else
                    tab = _tabs.GetOrAdd(id, tab);
                result.Add(tab);
            }

            string visibleId = await GetVisibleTabIdAsync(result).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(visibleId)) _currentContextId = visibleId;
            else if (!result.Any(item => item.Id == _currentContextId)) _currentContextId = null;

            foreach (var item in result) item.IsCurrent = item.Id == _currentContextId;
            CurrentPage = string.IsNullOrEmpty(_currentContextId)
                ? null
                : await GetPageAsync(_currentContextId).ConfigureAwait(false);
            var activeIds = new HashSet<string>(result.Select(item => item.Id), StringComparer.Ordinal);
            foreach (string staleId in _pages.Keys.Concat(_tabs.Keys).Where(id => !activeIds.Contains(id)).Distinct().ToList())
            {
                if (_pages.TryRemove(staleId, out var stalePage)) stalePage.Dispose();
                _tabs.TryRemove(staleId, out _);
                if (CurrentPage?.TargetId == staleId) CurrentPage = null;
                if (_currentContextId == staleId) _currentContextId = null;
            }
            return new BrowserTabs(_browser, result);
        }

        private async Task<string> GetVisibleTabIdAsync(IList<BrowserTab> tabs)
        {
            var previous = tabs.FirstOrDefault(item => item.Id == _currentContextId);
            if (previous != null)
            {
                try
                {
                    object state = await (await GetPageAsync(previous.Id).ConfigureAwait(false)).EvaluateValueAsync("document.visibilityState").ConfigureAwait(false);
                    if (string.Equals(Convert.ToString(state), "visible", StringComparison.OrdinalIgnoreCase))
                        return previous.Id;
                }
                catch { }
            }

            foreach (var tab in tabs)
            {
                if (tab.Id == _currentContextId) continue;
                try
                {
                    object state = await (await GetPageAsync(tab.Id).ConfigureAwait(false)).EvaluateValueAsync("document.visibilityState").ConfigureAwait(false);
                    if (string.Equals(Convert.ToString(state), "visible", StringComparison.OrdinalIgnoreCase))
                        return tab.Id;
                }
                catch { }
            }
            return null;
        }

        public override BrowserPage GetPage(string targetId) => GetPageAsync(targetId).GetAwaiter().GetResult();

        public override async Task<BrowserPage> GetPageAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
                throw new ArgumentException("A browsing context identifier is required.", nameof(targetId));
            await _driver.EnsureSessionAsync().ConfigureAwait(false);
            if (_pages.TryGetValue(targetId, out var existing)) return existing;
            var page = new BrowserPage(new FirefoxPageSession(targetId, _driver.Connection));
            await page.InitializeAsync().ConfigureAwait(false);
            if (!_pages.TryAdd(targetId, page))
            {
                page.Dispose();
                page = _pages[targetId];
            }
            return page;
        }

        public override BrowserTab GetTab(string targetId) => GetTabAsync(targetId).GetAwaiter().GetResult();

        public override async Task<BrowserTab> GetTabAsync(string targetId)
        {
            var page = await GetPageAsync(targetId).ConfigureAwait(false);
            return GetTab(page);
        }

        private BrowserTab GetTab(BrowserPage page)
        {
            var tab = _tabs.GetOrAdd(page.TargetId, _ => new BrowserTab(_browser, page));
            tab.IsCurrent = page.TargetId == _currentContextId;
            return tab;
        }

        public override BrowserTab OpenTab(string url, int timeoutMilliseconds = 0) =>
            OpenTabAsync(url, timeoutMilliseconds).GetAwaiter().GetResult();

        public override async Task<BrowserTab> OpenTabAsync(string url, int timeoutMilliseconds = 0)
        {
            var page = await OpenAsync(url, timeoutMilliseconds).ConfigureAwait(false);
            return GetTab(page);
        }

        public override BrowserPage OpenPage(string url, int timeoutMilliseconds = 0) =>
            OpenAsync(url, timeoutMilliseconds).GetAwaiter().GetResult();

        public override async Task<BrowserPage> OpenAsync(string url, int timeoutMilliseconds = 0)
        {
            await _driver.EnsureSessionAsync().ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(url)) url = "about:blank";
            else if (_browser.Router != null) url = _browser.Router.Resolve(url);

            JObject created = await _driver.SendAsync("browsingContext.create", new JObject
            {
                ["type"] = "tab",
                ["background"] = false
            }).ConfigureAwait(false);
            string id = (string)created["context"];
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Firefox did not return the identifier of the new tab.");

            var page = await GetPageAsync(id).ConfigureAwait(false);
            try
            {
                if (!string.Equals(url, "about:blank", StringComparison.OrdinalIgnoreCase))
                    await page.NavigateAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : _driver.Timeout).ConfigureAwait(false);
                CurrentPage = page;
                _currentContextId = id;
                return page;
            }
            catch
            {
                try { await _driver.SendAsync("browsingContext.close", new JObject { ["context"] = id }).ConfigureAwait(false); }
                catch { }
                throw;
            }
        }

        public override string Go(string address, int timeoutMilliseconds = 0) =>
            GoAsync(address, timeoutMilliseconds).GetAwaiter().GetResult();

        public override async Task<string> GoAsync(string address, int timeoutMilliseconds = 0)
        {
            if (string.IsNullOrWhiteSpace(address)) return await GoBackAsync().ConfigureAwait(false);
            string url = _browser.Router == null ? address : _browser.Router.Resolve(address, CurrentPage?.Url);
            if (CurrentPage == null)
                return (await OpenAsync(url, timeoutMilliseconds).ConfigureAwait(false)).Url;
            return await CurrentPage.NavigateAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : _driver.Timeout).ConfigureAwait(false);
        }

        public override string Reload() => ReloadAsync().GetAwaiter().GetResult();

        public override async Task<string> ReloadAsync()
        {
            if (CurrentPage == null) return null;
            return await CurrentPage.ReloadAsync().ConfigureAwait(false);
        }

        public override string SetLocation(string address, bool addHistoryEntry) =>
            SetLocationAsync(address, addHistoryEntry).GetAwaiter().GetResult();

        public override async Task<string> SetLocationAsync(string address, bool addHistoryEntry)
        {
            if (CurrentPage == null) throw new InvalidOperationException("There is no current browser tab.");
            string url = _browser.Router == null ? address : _browser.Router.Resolve(address, CurrentPage.Url);
            string operation = addHistoryEntry ? "pushState" : "replaceState";
            return await CurrentPage.EvaluateAsync("history." + operation + "(null, null, " +
                Newtonsoft.Json.JsonConvert.ToString(url) + "); location.href").ConfigureAwait(false);
        }

        public override string GoBack() => GoBackAsync().GetAwaiter().GetResult();
        public override Task<string> GoBackAsync() => CurrentPage == null ? Task.FromResult<string>(null) : CurrentPage.GoBackAsync();
        public override string GoForward() => GoForwardAsync().GetAwaiter().GetResult();
        public override Task<string> GoForwardAsync() => CurrentPage == null ? Task.FromResult<string>(null) : CurrentPage.GoForwardAsync();

        public override BrowserPage Load(string url, int timeoutMilliseconds = 0) => LoadAsync(url, timeoutMilliseconds).GetAwaiter().GetResult();
        public override Task<BrowserPage> LoadAsync(string url, int timeoutMilliseconds = 0) =>
            OpenAsync(url, timeoutMilliseconds > 0 ? timeoutMilliseconds : _driver.Timeout);

        public override bool Activate(string targetId) => ActivateAsync(targetId).GetAwaiter().GetResult();

        public override async Task<bool> ActivateAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            var tabs = await GetTabsAsync().ConfigureAwait(false);
            if (!tabs.Any(item => item.Id == targetId)) return false;
            await _driver.SendAsync("browsingContext.activate", new JObject { ["context"] = targetId }).ConfigureAwait(false);
            CurrentPage = await GetPageAsync(targetId).ConfigureAwait(false);
            _currentContextId = targetId;
            foreach (var tab in _tabs.Values) tab.IsCurrent = tab.Id == targetId;
            return true;
        }

        public override bool Close(string targetId) => CloseAsync(targetId).GetAwaiter().GetResult();

        public override async Task<bool> CloseAsync(string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId)) return false;
            var tabs = await GetTabsAsync().ConfigureAwait(false);
            int closingIndex = tabs.Select((tab, index) => new { tab, index })
                .Where(item => item.tab.Id == targetId).Select(item => item.index).DefaultIfEmpty(-1).First();
            if (closingIndex < 0) return false;
            bool wasCurrent = _currentContextId == targetId || CurrentPage?.TargetId == targetId;

            string replacementId = null;
            if (wasCurrent && tabs.Count == 1)
                replacementId = (await OpenTabAsync("about:blank").ConfigureAwait(false)).Id;

            await _driver.SendAsync("browsingContext.close", new JObject { ["context"] = targetId }).ConfigureAwait(false);
            if (_pages.TryRemove(targetId, out var page)) page.Dispose();
            _tabs.TryRemove(targetId, out _);
            if (CurrentPage?.TargetId == targetId) CurrentPage = null;

            if (wasCurrent)
            {
                var remaining = await GetTabsAsync().ConfigureAwait(false);
                if (string.IsNullOrEmpty(replacementId))
                    replacementId = remaining.FirstOrDefault(item => item.IsCurrent)?.Id;
                if (string.IsNullOrEmpty(replacementId))
                {
                    replacementId = remaining.ElementAtOrDefault(Math.Min(closingIndex, remaining.Count - 1))?.Id
                        ?? remaining.LastOrDefault()?.Id;
                    if (!string.IsNullOrEmpty(replacementId))
                        await _driver.SendAsync("browsingContext.activate", new JObject { ["context"] = replacementId }).ConfigureAwait(false);
                }
                if (!string.IsNullOrEmpty(replacementId))
                {
                    CurrentPage = await GetPageAsync(replacementId).ConfigureAwait(false);
                    _currentContextId = replacementId;
                    var refreshed = await GetTabsAsync().ConfigureAwait(false);
                    var visible = refreshed.FirstOrDefault(item => item.IsCurrent);
                    if (visible != null && visible.Id != replacementId)
                    {
                        CurrentPage = await GetPageAsync(visible.Id).ConfigureAwait(false);
                        _currentContextId = visible.Id;
                    }
                }
                else
                {
                    CurrentPage = null;
                    _currentContextId = null;
                }
            }
            return true;
        }
    }
}
