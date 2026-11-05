using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>Remote browser objects evaluated in the Chromium process.</summary>
    public abstract class BrowserRemoteObject
    {
        protected readonly BrowserTab Tab;
        protected BrowserRemoteObject(BrowserTab tab) { Tab = tab ?? throw new ArgumentNullException(nameof(tab)); }
        protected Task<string> EvaluateAsync(string expression) => Tab.Page.EvaluateAsync(expression);
        protected object EvaluateValue(string expression) => Tab.Page.EvaluateValueAsync(expression).GetAwaiter().GetResult();
        protected static string Json(string value) => Newtonsoft.Json.JsonConvert.SerializeObject(value);
    }

    public sealed class BrowserWindow : BrowserRemoteObject
    {
        internal BrowserWindow(BrowserTab tab) : base(tab)
        {
            Document = new BrowserDocument(tab);
            Location = new BrowserLocation(tab);
            History = new BrowserHistory(tab);
            Navigator = new BrowserNavigator(tab);
            Screen = new BrowserScreen(tab);
            Console = new BrowserConsole(tab);
            Cookies = new BrowserCookies(tab);
            Storage = new BrowserStorage(tab);
        }
        public BrowserDocument Document { get; }
        public BrowserLocation Location { get; }
        public BrowserHistory History { get; }
        public BrowserNavigator Navigator { get; }
        public BrowserScreen Screen { get; }
        public BrowserConsole Console { get; }
        public BrowserCookies Cookies { get; }
        public BrowserStorage Storage { get; }
        public object Name => EvaluateValue("window.name");
        public object InnerWidth => EvaluateValue("window.innerWidth");
        public object InnerHeight => EvaluateValue("window.innerHeight");
        public Task<string> GetNameAsync() => EvaluateAsync("window.name");
        public Task<string> GetInnerWidthAsync() => EvaluateAsync("window.innerWidth");
        public Task<string> GetInnerHeightAsync() => EvaluateAsync("window.innerHeight");
        public Task<string> Focus() => FocusAsync();
        public Task<string> ScrollTo(double x, double y) => ScrollToAsync(x, y);
        public Task<string> Open(string url = null, string target = "_blank") => OpenAsync(url, target);
        public Task<string> FocusAsync() => EvaluateAsync("(window.focus(), true)");
        public Task<string> ScrollToAsync(double x, double y) => EvaluateAsync("window.scrollTo(" + x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
        public Task<string> OpenAsync(string url = null, string target = "_blank") =>
            EvaluateAsync("window.open(" + Json(url == null ? "about:blank" : Tab.ResolveAddress(url)) + "," + Json(target) + ") !== null");
    }

    public sealed class BrowserDocument : BrowserRemoteObject
    {
        internal BrowserDocument(BrowserTab tab) : base(tab) { }
        public object Title => EvaluateValue("document.title");
        public object Url => EvaluateValue("document.URL");
        public object Cookie => EvaluateValue("document.cookie");
        public object ReadyState => EvaluateValue("document.readyState");
        public object VisibilityState => EvaluateValue("document.visibilityState");
        public object Html => EvaluateValue("document.documentElement ? document.documentElement.outerHTML : null");
        public object Text => EvaluateValue("document.documentElement ? document.documentElement.innerText : null");
        public Task<string> GetTitleAsync() => EvaluateAsync("document.title");
        public Task<string> GetUrlAsync() => EvaluateAsync("document.URL");
        public Task<string> GetReadyStateAsync() => EvaluateAsync("document.readyState");
        public Task<string> GetHtmlAsync() => EvaluateAsync("document.documentElement ? document.documentElement.outerHTML : null");
        public Task<string> GetTextAsync() => EvaluateAsync("document.documentElement ? document.documentElement.innerText : null");
        public Task<string> GetCookieAsync() => EvaluateAsync("document.cookie");
        public Task<string> QuerySelector(string selector) => QuerySelectorAsync(selector);
        public Task<string> QuerySelectorAll(string selector) => QuerySelectorAllAsync(selector);
        public Task<string> QuerySelectorAsync(string selector) =>
            EvaluateAsync("(function(e){return e?e.outerHTML:null})(document.querySelector(" + Json(selector) + "))");
        public Task<string> QuerySelectorAllAsync(string selector) =>
            EvaluateAsync("JSON.stringify(Array.from(document.querySelectorAll(" + Json(selector) + ")).map(e=>e.outerHTML))");
    }

    public sealed class BrowserLocation : BrowserRemoteObject
    {
        internal BrowserLocation(BrowserTab tab) : base(tab) { }
        public object Href => EvaluateValue("location.href");
        public object Origin => EvaluateValue("location.origin");
        public object Pathname => EvaluateValue("location.pathname");
        public object Search => EvaluateValue("location.search");
        public object Hash => EvaluateValue("location.hash");
        public Task<string> GetHrefAsync() => EvaluateAsync("location.href");
        public Task<string> GetOriginAsync() => EvaluateAsync("location.origin");
        public Task<string> GetPathnameAsync() => EvaluateAsync("location.pathname");
        public Task<string> GetSearchAsync() => EvaluateAsync("location.search");
        public Task<string> GetHashAsync() => EvaluateAsync("location.hash");
        public Task<string> Assign(string url) => AssignAsync(url);
        public Task<string> Replace(string url) => ReplaceAsync(url);
        public Task<string> AssignAsync(string url) => EvaluateAsync("(location.assign(" + Json(Tab.ResolveAddress(url)) + "), true)");
        public Task<string> ReplaceAsync(string url) => EvaluateAsync("(location.replace(" + Json(Tab.ResolveAddress(url)) + "), true)");
    }

    public sealed class BrowserHistory : BrowserRemoteObject
    {
        internal BrowserHistory(BrowserTab tab) : base(tab) { }
        public object Length => EvaluateValue("history.length");
        public object State => EvaluateValue("history.state");
        public Task<string> GetLengthAsync() => EvaluateAsync("history.length");
        public Task<string> GetStateAsync() => EvaluateAsync("JSON.stringify(history.state)");
        public Task<string> Back() => BackAsync();
        public Task<string> Forward() => ForwardAsync();
        public Task<string> Go(int delta) => GoAsync(delta);
        public Task<string> BackAsync() => Tab.Page.GoBackAsync();
        public Task<string> ForwardAsync() => Tab.Page.GoForwardAsync();
        public Task<string> GoAsync(int delta) => Tab.Page.GoAsync(delta);
        private static string StateJson(string jsonState) =>
            string.IsNullOrWhiteSpace(jsonState) ? "null" :
            JToken.Parse(jsonState).ToString(Newtonsoft.Json.Formatting.None);
        public Task<string> PushStateAsync(string jsonState, string title = "", string url = null) =>
            EvaluateAsync("(history.pushState(" + StateJson(jsonState) + "," + Json(title) + (url == null ? "" : "," + Json(Tab.ResolveAddress(url))) + "), true)");
        public Task<string> ReplaceStateAsync(string jsonState, string title = "", string url = null) =>
            EvaluateAsync("(history.replaceState(" + StateJson(jsonState) + "," + Json(title) + (url == null ? "" : "," + Json(Tab.ResolveAddress(url))) + "), true)");
    }

    public sealed class BrowserNavigator : BrowserRemoteObject
    {
        internal BrowserNavigator(BrowserTab tab) : base(tab) { }
        public object UserAgent => EvaluateValue("navigator.userAgent");
        public object Language => EvaluateValue("navigator.language");
        public object Languages => EvaluateValue("navigator.languages");
        public object Platform => EvaluateValue("navigator.platform");
        public object Online => EvaluateValue("navigator.onLine");
        public object CookieEnabled => EvaluateValue("navigator.cookieEnabled");
        public Task<string> GetUserAgentAsync() => EvaluateAsync("navigator.userAgent");
        public Task<string> GetLanguageAsync() => EvaluateAsync("navigator.language");
        public Task<string> GetLanguagesAsync() => EvaluateAsync("JSON.stringify(navigator.languages)");
        public Task<string> GetPlatformAsync() => EvaluateAsync("navigator.platform");
        public Task<string> GetOnlineAsync() => EvaluateAsync("navigator.onLine");
        public Task<string> GetCookieEnabledAsync() => EvaluateAsync("navigator.cookieEnabled");
    }

    public sealed class BrowserScreen : BrowserRemoteObject
    {
        internal BrowserScreen(BrowserTab tab) : base(tab) { }
        public object Width => EvaluateValue("screen.width");
        public object Height => EvaluateValue("screen.height");
        public object AvailWidth => EvaluateValue("screen.availWidth");
        public object AvailHeight => EvaluateValue("screen.availHeight");
        public object ColorDepth => EvaluateValue("screen.colorDepth");
        public Task<string> GetWidthAsync() => EvaluateAsync("screen.width");
        public Task<string> GetHeightAsync() => EvaluateAsync("screen.height");
        public Task<string> GetAvailableWidthAsync() => EvaluateAsync("screen.availWidth");
        public Task<string> GetAvailableHeightAsync() => EvaluateAsync("screen.availHeight");
        public Task<string> GetColorDepthAsync() => EvaluateAsync("screen.colorDepth");
        public Task<string> GetPixelDepthAsync() => EvaluateAsync("screen.pixelDepth");
    }

    public sealed class BrowserConsole : BrowserRemoteObject
    {
        internal BrowserConsole(BrowserTab tab) : base(tab) { }
        public Task<string> Log(string value) => LogAsync(value);
        public Task<string> Info(string value) => InfoAsync(value);
        public Task<string> Warn(string value) => WarnAsync(value);
        public Task<string> Error(string value) => ErrorAsync(value);
        public Task<string> Clear() => ClearAsync();
        public Task<string> LogAsync(string value) => EvaluateAsync("console.log(" + Json(value) + ")");
        public Task<string> InfoAsync(string value) => EvaluateAsync("console.info(" + Json(value) + ")");
        public Task<string> WarnAsync(string value) => EvaluateAsync("console.warn(" + Json(value) + ")");
        public Task<string> ErrorAsync(string value) => EvaluateAsync("console.error(" + Json(value) + ")");
        public Task<string> ClearAsync() => EvaluateAsync("console.clear()");
    }

    public sealed class BrowserCookies : BrowserRemoteObject
    {
        internal BrowserCookies(BrowserTab tab) : base(tab) { }
        public object Document => EvaluateValue("document.cookie");
        public object All => BrowserPage.ToClrValue(Tab.Page.SendCommandAsync("Network.getAllCookies").GetAwaiter().GetResult()["cookies"]);
        public Task<bool> Set(string cookieJson) => SetAsync(cookieJson);
        public Task<JObject> Delete(string name, string domain = null, string path = null) => DeleteAsync(name, domain, path);
        public Task<JObject> Clear() => ClearAsync();
        public Task<string> GetDocumentCookiesAsync() => EvaluateAsync("document.cookie");
        public Task<string> SetDocumentCookieAsync(string cookie) => EvaluateAsync("(document.cookie=" + Json(cookie) + ")");
        public async Task<string> GetAllAsync()
        {
            var result = await Tab.Page.SendCommandAsync("Network.getAllCookies").ConfigureAwait(false);
            return result["cookies"]?.ToString(Newtonsoft.Json.Formatting.None) ?? "[]";
        }
        public async Task<bool> SetAsync(string cookieJson)
        {
            if (string.IsNullOrWhiteSpace(cookieJson)) throw new ArgumentException("Cookie JSON is required.", nameof(cookieJson));
            var result = await Tab.Page.SendCommandAsync("Network.setCookie", JObject.Parse(cookieJson)).ConfigureAwait(false);
            return result["success"] == null || result["success"].Value<bool>();
        }
        public Task<JObject> DeleteAsync(string name, string domain = null, string path = null)
        {
            var parameters = new JObject { ["name"] = name };
            if (domain != null) parameters["domain"] = domain;
            if (path != null) parameters["path"] = path;
            return Tab.Page.SendCommandAsync("Network.deleteCookies", parameters);
        }
        public Task<JObject> ClearAsync() => Tab.Page.SendCommandAsync("Network.clearBrowserCookies");
    }

    public sealed class BrowserStorage : BrowserRemoteObject
    {
        internal BrowserStorage(BrowserTab tab) : base(tab) { Local = new BrowserStorageArea(tab, "localStorage"); Session = new BrowserStorageArea(tab, "sessionStorage"); }
        public BrowserStorageArea Local { get; }
        public BrowserStorageArea Session { get; }
    }

    public sealed class BrowserStorageArea : BrowserRemoteObject
    {
        private readonly string _storage;
        internal BrowserStorageArea(BrowserTab tab, string storage) : base(tab) { _storage = storage; }
        public object Length => EvaluateValue(_storage + ".length");
        public object Keys => EvaluateValue("Object.keys(" + _storage + ")");
        public Task<string> GetItem(string key) => GetItemAsync(key);
        public Task<string> SetItem(string key, string value) => SetItemAsync(key, value);
        public Task<string> RemoveItem(string key) => RemoveItemAsync(key);
        public Task<string> Clear() => ClearAsync();
        public Task<string> GetLengthAsync() => EvaluateAsync(_storage + ".length");
        public Task<string> GetItemAsync(string key) => EvaluateAsync(_storage + ".getItem(" + Json(key) + ")");
        public Task<string> SetItemAsync(string key, string value) => EvaluateAsync("(" + _storage + ".setItem(" + Json(key) + "," + Json(value) + "), true)");
        public Task<string> RemoveItemAsync(string key) => EvaluateAsync("(" + _storage + ".removeItem(" + Json(key) + "), true)");
        public Task<string> ClearAsync() => EvaluateAsync("(" + _storage + ".clear(), true)");
        public Task<string> GetKeysAsync() => EvaluateAsync("JSON.stringify(Object.keys(" + _storage + "))");
    }

    public sealed class BrowserWindows
    {
        private readonly BrowserTab _tab;
        internal BrowserWindows(BrowserTab tab) { _tab = tab; }
        public object Length => _tab.Page.EvaluateValueAsync("window.frames.length").GetAwaiter().GetResult();
        public IList<BrowserFrame> All => GetAllAsync().GetAwaiter().GetResult();
        public Task<BrowserFrame> Get(int index) => GetAsync(index);
        public Task<string> GetCountAsync() => _tab.Page.EvaluateAsync("window.frames.length");
        public Task<string> GetNamesAsync() => _tab.Page.EvaluateAsync("JSON.stringify(Array.from(window.frames).map((w,i)=>({index:i,name:w.name,url:(()=>{try{return w.location.href}catch(e){return null}})()})))");
        public Task<string> GetFrameUrlAsync(int index) => _tab.Page.EvaluateAsync("(function(w){try{return w.location.href}catch(e){return null}})(window.frames[" + index + "])");
        public async Task<BrowserFrame> GetAsync(int index)
        {
            int count = int.Parse(await GetCountAsync().ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            return new BrowserFrame(_tab, index);
        }
        public async Task<IList<BrowserFrame>> GetAllAsync()
        {
            int count = int.Parse(await GetCountAsync().ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
            var frames = new List<BrowserFrame>(count);
            for (int i = 0; i < count; i++) frames.Add(new BrowserFrame(_tab, i));
            return frames;
        }
    }

    /// <summary>A lightweight index-based proxy to a child browsing context.</summary>
    public sealed class BrowserFrame
    {
        private readonly BrowserTab _tab;
        public int Index { get; }
        internal BrowserFrame(BrowserTab tab, int index) { _tab = tab; Index = index; }
        private string Prefix => "window.frames[" + Index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
        public object Name => _tab.Page.EvaluateValueAsync(Prefix + ".name").GetAwaiter().GetResult();
        public object Url => _tab.Page.EvaluateValueAsync("(function(w){try{return w.location.href}catch(e){return null}})(" + Prefix + ")").GetAwaiter().GetResult();
        public object Title => _tab.Page.EvaluateValueAsync("(function(w){try{return w.document.title}catch(e){return null}})(" + Prefix + ")").GetAwaiter().GetResult();
        public Task<string> EvaluateAsync(string expression) =>
            _tab.Page.EvaluateAsync("(function(w){return w.eval(" + (expression ?? "undefined") + ")})(" + Prefix + ")");
    }
}
