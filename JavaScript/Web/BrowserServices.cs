using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    public abstract class BrowserService
    {
        protected readonly BrowserProvider Browser;
        protected BrowserService(BrowserProvider browser) { Browser = browser ?? throw new ArgumentNullException(nameof(browser)); }
        protected Task<JObject> SendAsync(string method, JObject parameters = null) => Browser.SendBrowserCommandAsync(method, parameters);
    }

    public sealed class BrowserDownloads : BrowserService
    {
        internal BrowserDownloads(BrowserProvider browser) : base(browser) { }
        public Task<JObject> AllowAsync(string directory) => SendAsync("Browser.setDownloadBehavior",
            new JObject { ["behavior"] = "allow", ["downloadPath"] = directory, ["eventsEnabled"] = true });
        public Task<JObject> DenyAsync() => SendAsync("Browser.setDownloadBehavior",
            new JObject { ["behavior"] = "deny", ["eventsEnabled"] = true });
        public Task<JObject> AllowAndNameAsync(string directory) => SendAsync("Browser.setDownloadBehavior",
            new JObject { ["behavior"] = "allowAndName", ["downloadPath"] = directory, ["eventsEnabled"] = true });
        public Task<JObject> CancelAsync(string guid) => SendAsync("Browser.cancelDownload",
            new JObject { ["guid"] = guid });
    }

    public sealed class BrowserPermissions : BrowserService
    {
        internal BrowserPermissions(BrowserProvider browser) : base(browser) { }
        public Task<JObject> GrantAsync(string origin, IEnumerable<string> permissions)
        {
            if (string.IsNullOrWhiteSpace(origin)) throw new ArgumentException("An origin is required.", nameof(origin));
            if (permissions == null) throw new ArgumentNullException(nameof(permissions));
            return SendAsync("Browser.grantPermissions", new JObject
            {
                ["origin"] = origin,
                ["permissions"] = new JArray(permissions)
            });
        }
        public Task<JObject> ResetAsync(string origin = null)
        {
            var parameters = new JObject();
            if (!string.IsNullOrWhiteSpace(origin)) parameters["origin"] = origin;
            return SendAsync("Browser.resetPermissions", parameters);
        }
    }

    public sealed class BrowserRuntime : BrowserService
    {
        internal BrowserRuntime(BrowserProvider browser) : base(browser) { }
        public Task<JObject> GetVersionAsync() => SendAsync("Browser.getVersion");
        public Task<JObject> GetCommandLineAsync() => SendAsync("Browser.getBrowserCommandLine");
        public Task<JObject> CloseAsync() => SendAsync("Browser.close");
    }
}
