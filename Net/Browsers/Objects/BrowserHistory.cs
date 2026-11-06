using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserHistory : BrowserRemote
    {
        internal BrowserHistory(BrowserTab tab) : base(tab) { }
        public object Length => EvaluateValue("history.length");
        public object State => EvaluateValue("history.state");
        public Task<string> GetLengthAsync() => EvaluateAsync("history.length");
        public Task<string> GetStateAsync() => EvaluateAsync("JSON.stringify(history.state)");
        public string Back() => BackAsync().GetAwaiter().GetResult();
        public string Forward() => ForwardAsync().GetAwaiter().GetResult();
        public string Go(int delta) => GoAsync(delta).GetAwaiter().GetResult();
        public Task<string> BackAsync() => Tab.Page.GoBackAsync();
        public Task<string> ForwardAsync() => Tab.Page.GoForwardAsync();
        public Task<string> GoAsync(int delta) => Tab.Page.GoAsync(delta);
        private static string StateJson(string jsonState) =>
            string.IsNullOrWhiteSpace(jsonState) ? "null" :
            JToken.Parse(jsonState).ToString(Newtonsoft.Json.Formatting.None);
        public Task<string> PushStateAsync(string jsonState, string title = "", string url = null) =>
            EvaluateAsync("(history.pushState(" + StateJson(jsonState) + "," + Json(title) + (url == null ? "" : "," + Json(Tab.ResolveAddress(url))) + "), true)");
        public Task<string> ReplaceStateAsync(string jsonState, string title = "", string url = null) =>
            EvaluateAsync("(history.replaceState(" + StateJson(jsonState) + "," + Json(title) + (url == null ? "" : "," + Json(Tab.ResolveAddress(url))) + "), true)");
    }
}
