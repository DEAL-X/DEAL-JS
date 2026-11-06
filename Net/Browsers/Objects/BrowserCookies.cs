using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserCookies : BrowserRemote
    {

        public object Document => EvaluateValue("document.cookie");
        public object All => BrowserPage.ToClrValue(Tab.Page.SendCommand("Network.getAllCookies")["cookies"]);
        internal BrowserCookies(BrowserTab tab) : base(tab) { }

        public string GetDocumentCookies() => GetDocumentCookiesAsync().GetAwaiter().GetResult();
        public Task<string> GetDocumentCookiesAsync() => EvaluateAsync("document.cookie");
        public string SetDocumentCookie(string cookie) => SetDocumentCookieAsync(cookie).GetAwaiter().GetResult();
        public Task<string> SetDocumentCookieAsync(string cookie) => EvaluateAsync("(document.cookie=" + Json(cookie) + ")");
        public string GetAll() => GetAllAsync().GetAwaiter().GetResult();
        public async Task<string> GetAllAsync()
        {
            var result = await Tab.Page.SendCommandAsync("Network.getAllCookies").ConfigureAwait(false);
            return result["cookies"]?.ToString(Newtonsoft.Json.Formatting.None) ?? "[]";
        }
        public bool Set(string cookieJson) => SetAsync(cookieJson).GetAwaiter().GetResult();
        public async Task<bool> SetAsync(string cookieJson)
        {
            if (string.IsNullOrWhiteSpace(cookieJson)) throw new ArgumentException("Cookie JSON is required.", nameof(cookieJson));
            var result = await Tab.Page.SendCommandAsync("Network.setCookie", JObject.Parse(cookieJson)).ConfigureAwait(false);
            return result["success"] == null || result["success"].Value<bool>();
        }
        public JObject Delete(string name, string domain = null, string path = null) => DeleteAsync(name, domain, path).GetAwaiter().GetResult();
        public Task<JObject> DeleteAsync(string name, string domain = null, string path = null)
        {
            var parameters = new JObject { ["name"] = name };
            if (domain != null) parameters["domain"] = domain;
            if (path != null) parameters["path"] = path;
            return Tab.Page.SendCommandAsync("Network.deleteCookies", parameters);
        }
        public JObject Clear() => ClearAsync().GetAwaiter().GetResult();
        public Task<JObject> ClearAsync() => Tab.Page.SendCommandAsync("Network.clearBrowserCookies");
    }
}
