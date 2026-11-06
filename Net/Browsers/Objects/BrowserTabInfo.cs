using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserTabInfo
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string WebSocketDebuggerUrl { get; set; }
        public bool IsCurrent { get; set; }
    }

}