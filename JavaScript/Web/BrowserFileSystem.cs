using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>
    /// A proxy for a FileSystemFileHandle that remains in Chromium. The handle is
    /// deliberately not copied into ClearScript because it is a browser-owned object.
    /// </summary>
    public sealed class BrowserFileSystemFileHandle : IDisposable
    {
        private readonly BrowserPage _page;
        private bool _disposed;
        internal BrowserFileSystemFileHandle(BrowserPage page, string objectId)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            ObjectId = objectId ?? throw new ArgumentNullException(nameof(objectId));
        }

        internal string ObjectId { get; }
        public Task<string> name => GetNameAsync();
        public Task<string> kind => GetKindAsync();
        public Task<string> GetNameAsync() => _page.CallFunctionValueAsync(ObjectId, "function(){return this.name}");
        public Task<string> GetKindAsync() => _page.CallFunctionValueAsync(ObjectId, "function(){return this.kind}");
        public Task<BrowserFileSystemFile> getFile() => GetFileAsync();
        public Task<string> text() => ReadTextAsync();
        public Task<string> base64() => ReadBase64Async();
        public Task<bool> writeText(string text) => WriteTextAsync(text);
        public Task<BrowserFileSystemWritable> createWritable() => CreateWritableAsync();
        public Task<string> queryPermission(string mode = "read") => PermissionAsync("queryPermission", mode);
        public Task<string> requestPermission(string mode = "readwrite") => PermissionAsync("requestPermission", mode);
        private Task<string> PermissionAsync(string method, string mode) =>
            _page.CallFunctionValueAsync(ObjectId, "async function(value){return await this[" + Newtonsoft.Json.JsonConvert.SerializeObject(method) + "]({mode:value})}",
                new JArray { new JObject { ["value"] = mode ?? "read" } });
        public Task<bool> isSameEntry(BrowserFileSystemFileHandle other) => IsSameEntryAsync(other);
        public async Task<bool> IsSameEntryAsync(BrowserFileSystemFileHandle other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (!ReferenceEquals(_page, other._page))
                throw new InvalidOperationException("File handles from different browser tabs cannot be compared.");
            string result = await _page.CallFunctionValueAsync(ObjectId, "async function(other){return await this.isSameEntry(other)}",
                new JArray { new JObject { ["objectId"] = other.ObjectId } }).ConfigureAwait(false);
            return bool.TryParse(result, out bool same) && same;
        }

        public Task<BrowserFileSystemFile> GetFileAsync() =>
            _page.CallFunctionObjectAsync(ObjectId, "async function(){return await this.getFile()}")
                .ContinueWith(t => new BrowserFileSystemFile(_page, t.GetAwaiter().GetResult()), TaskScheduler.Default);

        public Task<string> ReadTextAsync() =>
            _page.CallFunctionValueAsync(ObjectId, "async function(){return await (await this.getFile()).text()}");

        public Task<string> ReadBase64Async() =>
            _page.CallFunctionValueAsync(ObjectId,
                "async function(){const b=new Uint8Array(await (await this.getFile()).arrayBuffer());let s='';for(let i=0;i<b.length;i+=0x8000)s+=String.fromCharCode.apply(null,b.subarray(i,i+0x8000));return btoa(s)}");

        public async Task<bool> WriteTextAsync(string text)
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(value){const w=await this.createWritable();try{await w.write(value);await w.close();return true}catch(e){try{await w.abort()}catch(_){}throw e}}",
                new JArray { new JObject { ["value"] = text ?? string.Empty } }).ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }

        public Task<BrowserFileSystemWritable> CreateWritableAsync() =>
            _page.CallFunctionObjectAsync(ObjectId, "async function(){return await this.createWritable()}").ContinueWith(
                t => new BrowserFileSystemWritable(_page, t.GetAwaiter().GetResult()), TaskScheduler.Default);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _page.ReleaseObjectAsync(ObjectId).GetAwaiter().GetResult();
        }
    }

    public sealed class BrowserFileSystemWritable : IDisposable
    {
        private readonly BrowserPage _page;
        private bool _disposed;
        internal BrowserFileSystemWritable(BrowserPage page, string objectId)
        {
            _page = page;
            ObjectId = objectId;
        }
        internal string ObjectId { get; }
        public async Task<bool> WriteTextAsync(string text)
        {
            string result = await _page.CallFunctionValueAsync(ObjectId, "async function(value){await this.write(value);return true}",
                new JArray { new JObject { ["value"] = text ?? string.Empty } }).ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }
        public async Task<bool> WriteBase64Async(string base64)
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(value){const b=Uint8Array.from(atob(value),c=>c.charCodeAt(0));await this.write(b);return true}",
                new JArray { new JObject { ["value"] = base64 ?? string.Empty } }).ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }
        public Task<bool> write(string text) => WriteTextAsync(text);
        public Task<bool> writeBase64(string base64) => WriteBase64Async(base64);
        public Task<bool> close() => CloseAsync();
        public Task<bool> abort() => AbortAsync();
        public async Task<bool> CloseAsync()
        {
            string result = await _page.CallFunctionValueAsync(ObjectId, "async function(){await this.close();return true}").ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }
        public async Task<bool> AbortAsync()
        {
            string result = await _page.CallFunctionValueAsync(ObjectId, "async function(){await this.abort();return true}").ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _page.ReleaseObjectAsync(ObjectId).GetAwaiter().GetResult();
        }
    }

    public sealed class BrowserFileSystemFile : IDisposable
    {
        private readonly BrowserPage _page;
        private bool _disposed;
        internal BrowserFileSystemFile(BrowserPage page, string objectId) { _page = page; ObjectId = objectId; }
        internal string ObjectId { get; }
        public Task<string> name => _page.CallFunctionValueAsync(ObjectId, "function(){return this.name}");
        public Task<string> type => _page.CallFunctionValueAsync(ObjectId, "function(){return this.type}");
        public Task<string> size => _page.CallFunctionValueAsync(ObjectId, "function(){return this.size}");
        public Task<string> lastModified => _page.CallFunctionValueAsync(ObjectId, "function(){return this.lastModified}");
        public Task<string> text() => _page.CallFunctionValueAsync(ObjectId, "async function(){return await this.text()}");
        public Task<string> base64() => _page.CallFunctionValueAsync(ObjectId,
            "async function(){const b=new Uint8Array(await this.arrayBuffer());let s='';for(let i=0;i<b.length;i+=0x8000)s+=String.fromCharCode.apply(null,b.subarray(i,i+0x8000));return btoa(s)}");
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _page.ReleaseObjectAsync(ObjectId).GetAwaiter().GetResult();
        }
    }

    public sealed class BrowserFileMetadata
    {
        public string name { get; set; }
        public long size { get; set; }
        public string type { get; set; }
        public long lastModified { get; set; }
    }
}
