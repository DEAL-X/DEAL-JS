using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserScreen : BrowserRemote
    {
        internal BrowserScreen(BrowserTab tab) : base(tab) { }
        public object Width => EvaluateValue("screen.width");
        public object Height => EvaluateValue("screen.height");
        public object AvailWidth => EvaluateValue("screen.availWidth");
        public object AvailHeight => EvaluateValue("screen.availHeight");
        public object ColorDepth => EvaluateValue("screen.colorDepth");
        public Task<string> GetWidthAsync() => EvaluateAsync("screen.width");
        public Task<string> GetHeightAsync() => EvaluateAsync("screen.height");
        public Task<string> GetAvailableWidthAsync() => EvaluateAsync("screen.availWidth");
        public Task<string> GetAvailableHeightAsync() => EvaluateAsync("screen.availHeight");
        public Task<string> GetColorDepthAsync() => EvaluateAsync("screen.colorDepth");
        public Task<string> GetPixelDepthAsync() => EvaluateAsync("screen.pixelDepth");
    }
}
