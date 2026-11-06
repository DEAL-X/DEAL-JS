using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Services
{
    public sealed class BrowserDownloads : BrowserService
    {
        internal BrowserDownloads(Browser browser) : base(browser) { }
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
}
