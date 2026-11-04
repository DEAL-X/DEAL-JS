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
        public Task<string> GetNameAsync() => EvaluateAsync("window.name");
        public Task<string> GetInnerWidthAsync() => EvaluateAsync("window.innerWidth");
        public Task<string> GetInnerHeightAsync() => EvaluateAsync("window.innerHeight");
        public Task<string> FocusAsync() => EvaluateAsync("(window.focus(), true)");
        public Task<string> ScrollToAsync(double x, double y) => EvaluateAsync("window.scrollTo(" + x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
        public Task<string> OpenAsync(string url = null, string target = "_blank") =>
            EvaluateAsync("window.open(" + Json(url ?? "about:blank") + "," + Json(target) + ") !== null");
    }

    public sealed class BrowserDocument : BrowserRemoteObject
    {
        internal BrowserDocument(BrowserTab tab) : base(tab) { }
        public Task<string> GetTitleAsync() => EvaluateAsync("document.title");
        public Task<string> GetUrlAsync() => EvaluateAsync("document.URL");
        public Task<string> GetReadyStateAsync() => EvaluateAsync("document.readyState");
        public Task<string> GetHtmlAsync() => EvaluateAsync("document.documentElement ? document.documentElement.outerHTML : null");
        public Task<string> GetTextAsync() => EvaluateAsync("document.documentElement ? document.documentElement.innerText : null");
        public Task<string> GetCookieAsync() => EvaluateAsync("document.cookie");
        public Task<string> QuerySelectorAsync(string selector) =>
            EvaluateAsync("(function(e){return e?e.outerHTML:null})(document.querySelector(" + Json(selector) + "))");
        public Task<string> QuerySelectorAllAsync(string selector) =>
            EvaluateAsync("JSON.stringify(Array.from(document.querySelectorAll(" + Json(selector) + ")).map(e=>e.outerHTML))");
    }

    public sealed class BrowserLocation : BrowserRemoteObject
    {
        internal BrowserLocation(BrowserTab tab) : base(tab) { }
        public Task<string> GetHrefAsync() => EvaluateAsync("location.href");
        public Task<string> GetOriginAsync() => EvaluateAsync("location.origin");
        public Task<string> GetPathnameAsync() => EvaluateAsync("location.pathname");
        public Task<string> GetSearchAsync() => EvaluateAsync("location.search");
        public Task<string> GetHashAsync() => EvaluateAsync("location.hash");
        public Task<string> AssignAsync(string url) => EvaluateAsync("(location.assign(" + Json(url) + "), true)");
        public Task<string> ReplaceAsync(string url) => EvaluateAsync("(location.replace(" + Json(url) + "), true)");
    }

    public sealed class BrowserHistory : BrowserRemoteObject
    {
        internal BrowserHistory(BrowserTab tab) : base(tab) { }
        public Task<string> GetLengthAsync() => EvaluateAsync("history.length");
        public Task<string> GetStateAsync() => EvaluateAsync("JSON.stringify(history.state)");
        public Task<string> BackAsync() => Tab.Page.GoBackAsync();
        public Task<string> ForwardAsync() => Tab.Page.GoForwardAsync();
        public Task<string> GoAsync(int delta) => EvaluateAsync("(history.go(" + delta + "), true)");
        public Task<string> PushStateAsync(string jsonState, string title = "", string url = null) =>
            EvaluateAsync("(history.pushState(" + (jsonState ?? "null") + "," + Json(title) + (url == null ? "" : "," + Json(url)) + "), true)");
        public Task<string> ReplaceStateAsync(string jsonState, string title = "", string url = null) =>
            EvaluateAsync("(history.replaceState(" + (jsonState ?? "null") + "," + Json(title) + (url == null ? "" : "," + Json(url)) + "), true)");
    }

    public sealed class BrowserNavigator : BrowserRemoteObject
    {
        internal BrowserNavigator(BrowserTab tab) : base(tab) { }
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
        public Task<string> LogAsync(string value) => EvaluateAsync("console.log(" + Json(value) + ")");
        public Task<string> InfoAsync(string value) => EvaluateAsync("console.info(" + Json(value) + ")");
        public Task<string> WarnAsync(string value) => EvaluateAsync("console.warn(" + Json(value) + ")");
        public Task<string> ErrorAsync(string value) => EvaluateAsync("console.error(" + Json(value) + ")");
        public Task<string> ClearAsync() => EvaluateAsync("console.clear()");
    }

    public sealed class BrowserCookies : BrowserRemoteObject
    {
        internal BrowserCookies(BrowserTab tab) : base(tab) { }
        public Task<string> GetDocumentCookiesAsync() => EvaluateAsync("document.cookie");
        public Task<string> SetDocumentCookieAsync(string cookie) => EvaluateAsync("(document.cookie=" + Json(cookie) + ")");
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
        public Task<string> GetCountAsync() => _tab.Page.EvaluateAsync("window.frames.length");
        public Task<string> GetNamesAsync() => _tab.Page.EvaluateAsync("JSON.stringify(Array.from(window.frames).map((w,i)=>({index:i,name:w.name,url:(()=>{try{return w.location.href}catch(e){return null}})()})))");
        public Task<string> GetFrameUrlAsync(int index) => _tab.Page.EvaluateAsync("(function(w){try{return w.location.href}catch(e){return null}})(window.frames[" + index + "])");
    }
}
