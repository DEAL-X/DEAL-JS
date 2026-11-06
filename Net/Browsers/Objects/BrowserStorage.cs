using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserStorage : BrowserRemote
    {
        internal BrowserStorage(BrowserTab tab) : base(tab) { Local = new BrowserStorageArea(tab, "localStorage"); Session = new BrowserStorageArea(tab, "sessionStorage"); }
        public BrowserStorageArea Local { get; }
        public BrowserStorageArea Session { get; }
    }
}
