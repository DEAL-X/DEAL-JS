using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserNavigator : BrowserRemote
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
}
