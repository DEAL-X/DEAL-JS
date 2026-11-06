using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    /// <summary>A lightweight index-based proxy to a child browsing context.</summary>
    public sealed class BrowserFrame
    {
        private readonly BrowserTab _tab;
        public int Index { get; }
        internal BrowserFrame(BrowserTab tab, int index) { _tab = tab; Index = index; }
        private string Prefix => "window.frames[" + Index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
        public object Name => _tab.Page.EvaluateValueAsync(Prefix + ".name").GetAwaiter().GetResult();
        public object Url => _tab.Page.EvaluateValueAsync("(function(w){try{return w.location.href}catch(e){return null}})(" + Prefix + ")").GetAwaiter().GetResult();
        public object Title => _tab.Page.EvaluateValueAsync("(function(w){try{return w.document.title}catch(e){return null}})(" + Prefix + ")").GetAwaiter().GetResult();
        public Task<string> EvaluateAsync(string expression) =>
            _tab.Page.EvaluateAsync("(function(w){return w.eval(" + (expression ?? "undefined") + ")})(" + Prefix + ")");
    }
}
