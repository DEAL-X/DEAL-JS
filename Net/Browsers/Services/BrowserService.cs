using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MiMFa.Net.Browsers;

namespace MiMFa.Net.Browsers.Services
{
    public abstract class BrowserService
    {
        protected readonly Browser Browser;
        protected BrowserService(Browser browser) { Browser = browser ?? throw new ArgumentNullException(nameof(browser)); }
        protected JObject Send(string method, JObject parameters = null) => Browser.SendBrowserCommand(method, parameters);
        protected Task<JObject> SendAsync(string method, JObject parameters = null) => Browser.SendBrowserCommandAsync(method, parameters);
    }
}
