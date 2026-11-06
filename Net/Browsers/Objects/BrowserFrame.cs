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

        private string Prefix => "window.frames[" + Index.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]";
        public object Name => _tab.Page.EvaluateValue(Prefix + ".name");
        public object Url => _tab.Page.EvaluateValue("(function(w){try{return w.location.href}catch(e){return null}})(" + Prefix + ")");
        public object Title => _tab.Page.EvaluateValue("(function(w){try{return w.document.title}catch(e){return null}})(" + Prefix + ")");
        internal BrowserFrame(BrowserTab tab, int index) { _tab = tab; Index = index; }

        public string Evaluate(string expression) => EvaluateAsync(expression).GetAwaiter().GetResult();
        public Task<string> EvaluateAsync(string expression) =>
            _tab.Page.EvaluateAsync("(function(w){return w.eval(" + (expression ?? "undefined") + ")})(" + Prefix + ")");
    }
}
