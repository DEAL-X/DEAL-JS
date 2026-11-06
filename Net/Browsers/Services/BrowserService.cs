using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Services
{
    public abstract class BrowserService
    {
        protected readonly Browser Browser;
        protected BrowserService(Browser browser) { Browser = browser ?? throw new ArgumentNullException(nameof(browser)); }
        protected Task<JObject> SendAsync(string method, JObject parameters = null) => Browser.SendBrowserCommandAsync(method, parameters);
    }
}
