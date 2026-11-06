using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserLocation : BrowserRemote
    {

        public object Href => EvaluateValue("location.href");
        public object Origin => EvaluateValue("location.origin");
        public object Pathname => EvaluateValue("location.pathname");
        public object Search => EvaluateValue("location.search");
        public object Hash => EvaluateValue("location.hash");
        internal BrowserLocation(BrowserTab tab) : base(tab) { }

        public string GetHref() => GetHrefAsync().GetAwaiter().GetResult();
        public Task<string> GetHrefAsync() => EvaluateAsync("location.href");
        public string GetOrigin() => GetOriginAsync().GetAwaiter().GetResult();
        public Task<string> GetOriginAsync() => EvaluateAsync("location.origin");
        public string GetPathname() => GetPathnameAsync().GetAwaiter().GetResult();
        public Task<string> GetPathnameAsync() => EvaluateAsync("location.pathname");
        public string GetSearch() => GetSearchAsync().GetAwaiter().GetResult();
        public Task<string> GetSearchAsync() => EvaluateAsync("location.search");
        public string GetHash() => GetHashAsync().GetAwaiter().GetResult();
        public Task<string> GetHashAsync() => EvaluateAsync("location.hash");
        public string Assign(string url) => AssignAsync(url).GetAwaiter().GetResult();
        public Task<string> AssignAsync(string url) => EvaluateAsync("(location.assign(" + Json(Tab.ResolveAddress(url)) + "), true)");
        public string Replace(string url) => ReplaceAsync(url).GetAwaiter().GetResult();
        public Task<string> ReplaceAsync(string url) => EvaluateAsync("(location.replace(" + Json(Tab.ResolveAddress(url)) + "), true)");
    }
}
