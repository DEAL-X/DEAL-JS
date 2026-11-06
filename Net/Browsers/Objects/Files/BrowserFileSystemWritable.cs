using MiMFa.Net.Browsers.Objects;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects.Files
{
    public sealed class BrowserFileSystemWritable : IDisposable
    {
        private readonly BrowserPage _page;
        private bool _disposed;
        internal string ObjectId { get; }

        internal BrowserFileSystemWritable(BrowserPage page, string objectId)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            ObjectId = objectId ?? throw new ArgumentNullException(nameof(objectId));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _page.ReleaseObject(ObjectId);
        }

        public bool WriteText(string text) => WriteTextAsync(text).GetAwaiter().GetResult();
        public async Task<bool> WriteTextAsync(string text)
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(value){await this.write(value);return true}",
                new JArray { new JObject { ["value"] = text ?? string.Empty } }).ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }

        public bool WriteBase64(string base64) => WriteBase64Async(base64).GetAwaiter().GetResult();
        public async Task<bool> WriteBase64Async(string base64)
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(value){const b=Uint8Array.from(atob(value),c=>c.charCodeAt(0));await this.write(b);return true}",
                new JArray { new JObject { ["value"] = base64 ?? string.Empty } }).ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }

        public bool Close() => CloseAsync().GetAwaiter().GetResult();
        public async Task<bool> CloseAsync()
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(){await this.close();return true}").ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }

        public bool Abort() => AbortAsync().GetAwaiter().GetResult();
        public async Task<bool> AbortAsync()
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(){await this.abort();return true}").ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }
    }
}
