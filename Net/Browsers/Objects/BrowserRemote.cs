using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    /// <summary>Remote browser objects evaluated in the Chromium process.</summary>
    public abstract class BrowserRemote
    {
        protected readonly BrowserTab Tab;
        protected BrowserRemote(BrowserTab tab) { Tab = tab ?? throw new ArgumentNullException(nameof(tab)); }
        protected Task<string> EvaluateAsync(string expression) => Tab.Page.EvaluateAsync(expression);
        protected object EvaluateValue(string expression) => Tab.Page.EvaluateValueAsync(expression).GetAwaiter().GetResult();
        protected static string Json(string value) => Newtonsoft.Json.JsonConvert.SerializeObject(value);
    }
}
