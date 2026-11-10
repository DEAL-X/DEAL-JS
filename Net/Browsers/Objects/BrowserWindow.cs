using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserWindow : BrowserRemote
    {
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
        public bool IsActive => IsActiveAsync().GetAwaiter().GetResult();
        public bool IsLoaded => Document.IsLoaded;

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

        public Task<bool> IsActiveAsync() => IsActiveCoreAsync();
        private async Task<bool> IsActiveCoreAsync()
        {
            string result = await EvaluateAsync("document.hasFocus() && document.visibilityState === 'visible'").ConfigureAwait(false);
            return bool.TryParse(result, out var active) && active;
        }
        public Task<bool> IsLoadedAsync() => Document.IsLoadedAsync();
        public string GetName() => GetNameAsync().GetAwaiter().GetResult();
        public Task<string> GetNameAsync() => EvaluateAsync("window.name");
        public string GetInnerWidth() => GetInnerWidthAsync().GetAwaiter().GetResult();
        public Task<string> GetInnerWidthAsync() => EvaluateAsync("window.innerWidth");
        public string GetInnerHeight() => GetInnerHeightAsync().GetAwaiter().GetResult();
        public Task<string> GetInnerHeightAsync() => EvaluateAsync("window.innerHeight");
        public string Focus() => FocusAsync().GetAwaiter().GetResult();
        public Task<string> FocusAsync() => EvaluateAsync("(window.focus(), true)");
        public string ScrollTo(double x, double y) => ScrollToAsync(x, y).GetAwaiter().GetResult();
        public Task<string> ScrollToAsync(double x, double y) => EvaluateAsync("window.scrollTo(" + x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + y.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
        public string Open(string url = null, string target = "_blank") => OpenAsync(url, target).GetAwaiter().GetResult();
        public Task<string> OpenAsync(string url = null, string target = "_blank") =>
            EvaluateAsync("window.open(" + Json(url == null ? "about:blank" : Tab.ResolveAddress(url)) + "," + Json(target) + ") !== null");
    }
}
