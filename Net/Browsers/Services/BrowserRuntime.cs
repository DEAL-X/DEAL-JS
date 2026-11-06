using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Services
{
    public sealed class BrowserRuntime : BrowserService
    {
        internal BrowserRuntime(Browser browser) : base(browser) { }
        public JObject Version() => GetVersionAsync().GetAwaiter().GetResult();
        public Task<JObject> GetVersionAsync() => SendAsync("Browser.getVersion");
        public JObject CommandLine() => GetCommandLineAsync().GetAwaiter().GetResult();
        public Task<JObject> GetCommandLineAsync() => SendAsync("Browser.getBrowserCommandLine");
        public JObject Close() => CloseAsync().GetAwaiter().GetResult();
        public Task<JObject> CloseAsync() => SendAsync("Browser.close");
    }
}
