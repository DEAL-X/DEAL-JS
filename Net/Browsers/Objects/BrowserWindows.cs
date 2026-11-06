using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserWindows
    {
        private readonly BrowserTab _tab;
        internal BrowserWindows(BrowserTab tab) { _tab = tab; }
        public object Length => _tab.Page.EvaluateValueAsync("window.frames.length").GetAwaiter().GetResult();
        public IList<BrowserFrame> All => GetAllAsync().GetAwaiter().GetResult();
        public BrowserFrame Get(int index) => GetAsync(index).GetAwaiter().GetResult();
        public Task<string> GetCountAsync() => _tab.Page.EvaluateAsync("window.frames.length");
        public Task<string> GetNamesAsync() => _tab.Page.EvaluateAsync("JSON.stringify(Array.from(window.frames).map((w,i)=>({index:i,name:w.name,url:(()=>{try{return w.location.href}catch(e){return null}})()})))");
        public Task<string> GetFrameUrlAsync(int index) => _tab.Page.EvaluateAsync("(function(w){try{return w.location.href}catch(e){return null}})(window.frames[" + index + "])");
        public async Task<BrowserFrame> GetAsync(int index)
        {
            int count = int.Parse(await GetCountAsync().ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
            if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            return new BrowserFrame(_tab, index);
        }
        public async Task<IList<BrowserFrame>> GetAllAsync()
        {
            int count = int.Parse(await GetCountAsync().ConfigureAwait(false), System.Globalization.CultureInfo.InvariantCulture);
            var frames = new List<BrowserFrame>(count);
            for (int i = 0; i < count; i++) frames.Add(new BrowserFrame(_tab, i));
            return frames;
        }
    }
}
