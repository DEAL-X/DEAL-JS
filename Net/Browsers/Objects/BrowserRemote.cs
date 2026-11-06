using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    /// <summary>Remote browser objects evaluated in the selected browser page.</summary>
    public abstract class BrowserRemote
    {
        protected readonly BrowserTab Tab;
        protected BrowserRemote(BrowserTab tab) { Tab = tab ?? throw new ArgumentNullException(nameof(tab)); }
        protected string Evaluate(string expression) => EvaluateAsync(expression).GetAwaiter().GetResult();
        protected Task<string> EvaluateAsync(string expression) => Tab.Page.EvaluateAsync(expression);
        protected object EvaluateValue(string expression) => Tab.Page.EvaluateValue(expression);
        protected static string Json(string value) => Newtonsoft.Json.JsonConvert.SerializeObject(value);
    }
}
