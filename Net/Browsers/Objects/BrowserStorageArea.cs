using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    public sealed class BrowserStorageArea : BrowserRemote
    {
        private readonly string _storage;
        internal BrowserStorageArea(BrowserTab tab, string storage) : base(tab) { _storage = storage; }
        public object Length => EvaluateValue(_storage + ".length");
        public object Keys => EvaluateValue("Object.keys(" + _storage + ")");
        public string GetItem(string key) => GetItemAsync(key).GetAwaiter().GetResult();
        public string SetItem(string key, string value) => SetItemAsync(key, value).GetAwaiter().GetResult();
        public string RemoveItem(string key) => RemoveItemAsync(key).GetAwaiter().GetResult();
        public string Clear() => ClearAsync().GetAwaiter().GetResult();
        public Task<string> GetLengthAsync() => EvaluateAsync(_storage + ".length");
        public Task<string> GetItemAsync(string key) => EvaluateAsync(_storage + ".getItem(" + Json(key) + ")");
        public Task<string> SetItemAsync(string key, string value) => EvaluateAsync("(" + _storage + ".setItem(" + Json(key) + "," + Json(value) + "), true)");
        public Task<string> RemoveItemAsync(string key) => EvaluateAsync("(" + _storage + ".removeItem(" + Json(key) + "), true)");
        public Task<string> ClearAsync() => EvaluateAsync("(" + _storage + ".clear(), true)");
        public Task<string> GetKeysAsync() => EvaluateAsync("JSON.stringify(Object.keys(" + _storage + "))");
    }
}
