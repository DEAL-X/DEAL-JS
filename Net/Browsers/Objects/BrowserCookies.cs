using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserCookies : BrowserRemote
    {
        internal BrowserCookies(BrowserTab tab) : base(tab) { }
        public object Document => EvaluateValue("document.cookie");
        public object All => BrowserPage.ToClrValue(Tab.Page.SendCommandAsync("Network.getAllCookies").GetAwaiter().GetResult()["cookies"]);
        public bool Set(string cookieJson) => SetAsync(cookieJson).GetAwaiter().GetResult();
        public JObject Delete(string name, string domain = null, string path = null) => DeleteAsync(name, domain, path).GetAwaiter().GetResult();
        public JObject Clear() => ClearAsync().GetAwaiter().GetResult();
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
}
