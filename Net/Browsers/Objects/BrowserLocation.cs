using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserLocation : BrowserRemote
    {
        internal BrowserLocation(BrowserTab tab) : base(tab) { }
        public object Href => EvaluateValue("location.href");
        public object Origin => EvaluateValue("location.origin");
        public object Pathname => EvaluateValue("location.pathname");
        public object Search => EvaluateValue("location.search");
        public object Hash => EvaluateValue("location.hash");
        public Task<string> GetHrefAsync() => EvaluateAsync("location.href");
        public Task<string> GetOriginAsync() => EvaluateAsync("location.origin");
        public Task<string> GetPathnameAsync() => EvaluateAsync("location.pathname");
        public Task<string> GetSearchAsync() => EvaluateAsync("location.search");
        public Task<string> GetHashAsync() => EvaluateAsync("location.hash");
        public string Assign(string url) => AssignAsync(url).GetAwaiter().GetResult();
        public string Replace(string url) => ReplaceAsync(url).GetAwaiter().GetResult();
        public Task<string> AssignAsync(string url) => EvaluateAsync("(location.assign(" + Json(Tab.ResolveAddress(url)) + "), true)");
        public Task<string> ReplaceAsync(string url) => EvaluateAsync("(location.replace(" + Json(Tab.ResolveAddress(url)) + "), true)");
    }
}
