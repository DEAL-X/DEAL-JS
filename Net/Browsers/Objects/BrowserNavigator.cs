using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserNavigator : BrowserRemote
    {

        public object UserAgent => EvaluateValue("navigator.userAgent");
        public object Language => EvaluateValue("navigator.language");
        public object Languages => EvaluateValue("navigator.languages");
        public object Platform => EvaluateValue("navigator.platform");
        public object Online => EvaluateValue("navigator.onLine");
        public object CookieEnabled => EvaluateValue("navigator.cookieEnabled");
        internal BrowserNavigator(BrowserTab tab) : base(tab) { }

        public string GetUserAgent() => GetUserAgentAsync().GetAwaiter().GetResult();
        public Task<string> GetUserAgentAsync() => EvaluateAsync("navigator.userAgent");
        public string GetLanguage() => GetLanguageAsync().GetAwaiter().GetResult();
        public Task<string> GetLanguageAsync() => EvaluateAsync("navigator.language");
        public string GetLanguages() => GetLanguagesAsync().GetAwaiter().GetResult();
        public Task<string> GetLanguagesAsync() => EvaluateAsync("JSON.stringify(navigator.languages)");
        public string GetPlatform() => GetPlatformAsync().GetAwaiter().GetResult();
        public Task<string> GetPlatformAsync() => EvaluateAsync("navigator.platform");
        public string GetOnline() => GetOnlineAsync().GetAwaiter().GetResult();
        public Task<string> GetOnlineAsync() => EvaluateAsync("navigator.onLine");
        public string GetCookieEnabled() => GetCookieEnabledAsync().GetAwaiter().GetResult();
        public Task<string> GetCookieEnabledAsync() => EvaluateAsync("navigator.cookieEnabled");
    }
}
