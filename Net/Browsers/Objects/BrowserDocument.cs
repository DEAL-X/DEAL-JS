using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserDocument : BrowserRemote
    {
        internal BrowserDocument(BrowserTab tab) : base(tab) { }
        public object Title => EvaluateValue("document.title");
        public object Url => EvaluateValue("document.URL");
        public object Cookie => EvaluateValue("document.cookie");
        public object ReadyState => EvaluateValue("document.readyState");
        public object VisibilityState => EvaluateValue("document.visibilityState");
        public object Html => EvaluateValue("document.documentElement ? document.documentElement.outerHTML : null");
        public object Text => EvaluateValue("document.documentElement ? document.documentElement.innerText : null");
        public Task<string> GetTitleAsync() => EvaluateAsync("document.title");
        public Task<string> GetUrlAsync() => EvaluateAsync("document.URL");
        public Task<string> GetReadyStateAsync() => EvaluateAsync("document.readyState");
        public Task<string> GetHtmlAsync() => EvaluateAsync("document.documentElement ? document.documentElement.outerHTML : null");
        public Task<string> GetTextAsync() => EvaluateAsync("document.documentElement ? document.documentElement.innerText : null");
        public Task<string> GetCookieAsync() => EvaluateAsync("document.cookie");
        public string QuerySelector(string selector) => QuerySelectorAsync(selector).GetAwaiter().GetResult();
        public string QuerySelectorAll(string selector) => QuerySelectorAllAsync(selector).GetAwaiter().GetResult();
        public Task<string> QuerySelectorAsync(string selector) =>
            EvaluateAsync("(function(e){return e?e.outerHTML:null})(document.querySelector(" + Json(selector) + "))");
        public Task<string> QuerySelectorAllAsync(string selector) =>
            EvaluateAsync("JSON.stringify(Array.from(document.querySelectorAll(" + Json(selector) + ")).map(e=>e.outerHTML))");
    }
}
