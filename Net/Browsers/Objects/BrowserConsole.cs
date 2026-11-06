using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserConsole : BrowserRemote
    {
        internal BrowserConsole(BrowserTab tab) : base(tab) { }
        public string Log(string value) => LogAsync(value).GetAwaiter().GetResult();
        public Task<string> LogAsync(string value) => EvaluateAsync("console.log(" + Json(value) + ")");
        public string Info(string value) => InfoAsync(value).GetAwaiter().GetResult();
        public Task<string> InfoAsync(string value) => EvaluateAsync("console.info(" + Json(value) + ")");
        public string Warn(string value) => WarnAsync(value).GetAwaiter().GetResult();
        public Task<string> WarnAsync(string value) => EvaluateAsync("console.warn(" + Json(value) + ")");
        public string Error(string value) => ErrorAsync(value).GetAwaiter().GetResult();
        public Task<string> ErrorAsync(string value) => EvaluateAsync("console.error(" + Json(value) + ")");
        public string Clear() => ClearAsync().GetAwaiter().GetResult();
        public Task<string> ClearAsync() => EvaluateAsync("console.clear()");
    }
}
