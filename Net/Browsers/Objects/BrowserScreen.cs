using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserScreen : BrowserRemote
    {

        public object Width => EvaluateValue("screen.width");
        public object Height => EvaluateValue("screen.height");
        public object AvailWidth => EvaluateValue("screen.availWidth");
        public object AvailHeight => EvaluateValue("screen.availHeight");
        public object ColorDepth => EvaluateValue("screen.colorDepth");
        internal BrowserScreen(BrowserTab tab) : base(tab) { }

        public string GetWidth() => GetWidthAsync().GetAwaiter().GetResult();
        public Task<string> GetWidthAsync() => EvaluateAsync("screen.width");
        public string GetHeight() => GetHeightAsync().GetAwaiter().GetResult();
        public Task<string> GetHeightAsync() => EvaluateAsync("screen.height");
        public string GetAvailableWidth() => GetAvailableWidthAsync().GetAwaiter().GetResult();
        public Task<string> GetAvailableWidthAsync() => EvaluateAsync("screen.availWidth");
        public string GetAvailableHeight() => GetAvailableHeightAsync().GetAwaiter().GetResult();
        public Task<string> GetAvailableHeightAsync() => EvaluateAsync("screen.availHeight");
        public string GetColorDepth() => GetColorDepthAsync().GetAwaiter().GetResult();
        public Task<string> GetColorDepthAsync() => EvaluateAsync("screen.colorDepth");
        public string GetPixelDepth() => GetPixelDepthAsync().GetAwaiter().GetResult();
        public Task<string> GetPixelDepthAsync() => EvaluateAsync("screen.pixelDepth");
    }
}
