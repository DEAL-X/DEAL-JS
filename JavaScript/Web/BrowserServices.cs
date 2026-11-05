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
        public JObject Allow(string directory) => AllowAsync(directory).GetAwaiter().GetResult();
        public JObject Deny() => DenyAsync().GetAwaiter().GetResult();
        public JObject AllowAndName(string directory) => AllowAndNameAsync(directory).GetAwaiter().GetResult();
        public JObject Cancel(string guid) => CancelAsync(guid).GetAwaiter().GetResult();
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
        public JObject Grant(string origin, IEnumerable<string> permissions) => GrantAsync(origin, permissions).GetAwaiter().GetResult();
        public JObject Reset(string origin = null) => ResetAsync(origin).GetAwaiter().GetResult();
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
        public JObject Version() => GetVersionAsync().GetAwaiter().GetResult();
        public JObject CommandLine() => GetCommandLineAsync().GetAwaiter().GetResult();
        public JObject Close() => CloseAsync().GetAwaiter().GetResult();
        public Task<JObject> GetVersionAsync() => SendAsync("Browser.getVersion");
        public Task<JObject> GetCommandLineAsync() => SendAsync("Browser.getBrowserCommandLine");
        public Task<JObject> CloseAsync() => SendAsync("Browser.close");
    }
}
