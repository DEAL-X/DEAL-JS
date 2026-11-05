using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
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
        public string Name => GetNameAsync().GetAwaiter().GetResult();
        public string Kind => GetKindAsync().GetAwaiter().GetResult();
        public Task<string> GetNameAsync() => _page.CallFunctionValueAsync(ObjectId, "function(){return this.name}");
        public Task<string> GetKindAsync() => _page.CallFunctionValueAsync(ObjectId, "function(){return this.kind}");
        public Task<BrowserFileSystemFile> GetFile() => GetFileAsync();
        public Task<string> Text() => ReadTextAsync();
        public Task<string> Base64() => ReadBase64Async();
        public Task<bool> WriteText(string text) => WriteTextAsync(text);
        public Task<BrowserFileSystemWritable> CreateWritable() => CreateWritableAsync();
        public Task<string> QueryPermission(string mode = "read") => QueryPermissionAsync(mode);
        public Task<string> RequestPermission(string mode = "readwrite") => RequestPermissionAsync(mode);
        public Task<bool> IsSameEntry(BrowserFileSystemFileHandle other) => IsSameEntryAsync(other);
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
            _page.CallFunctionObjectAsync(ObjectId, "async function(){return await this.createWritable()}")
                .ContinueWith(t => new BrowserFileSystemWritable(_page, t.GetAwaiter().GetResult()), TaskScheduler.Default);
        public Task<string> QueryPermissionAsync(string mode = "read") => PermissionAsync("queryPermission", mode);
        public Task<string> RequestPermissionAsync(string mode = "readwrite") => PermissionAsync("requestPermission", mode);
        private Task<string> PermissionAsync(string method, string mode) =>
            _page.CallFunctionValueAsync(ObjectId,
                "async function(value){return await this[" + Newtonsoft.Json.JsonConvert.SerializeObject(method) + "]({mode:value})}",
                new JArray { new JObject { ["value"] = mode ?? "read" } });
        public async Task<bool> IsSameEntryAsync(BrowserFileSystemFileHandle other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (!ReferenceEquals(_page, other._page))
                throw new InvalidOperationException("File handles from different browser tabs cannot be compared.");
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(other){return await this.isSameEntry(other)}",
                new JArray { new JObject { ["objectId"] = other.ObjectId } }).ConfigureAwait(false);
            return bool.TryParse(result, out bool same) && same;
        }
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
            _page = page ?? throw new ArgumentNullException(nameof(page));
            ObjectId = objectId ?? throw new ArgumentNullException(nameof(objectId));
        }
        internal string ObjectId { get; }
        public Task<bool> WriteText(string text) => WriteTextAsync(text);
        public Task<bool> WriteBase64(string base64) => WriteBase64Async(base64);
        public Task<bool> Close() => CloseAsync();
        public Task<bool> Abort() => AbortAsync();
        public async Task<bool> WriteTextAsync(string text)
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(value){await this.write(value);return true}",
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
        public async Task<bool> CloseAsync()
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(){await this.close();return true}").ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }
        public async Task<bool> AbortAsync()
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(){await this.abort();return true}").ConfigureAwait(false);
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
        internal BrowserFileSystemFile(BrowserPage page, string objectId)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            ObjectId = objectId ?? throw new ArgumentNullException(nameof(objectId));
        }
        internal string ObjectId { get; }
        public string Name => _page.CallFunctionValueAsync(ObjectId, "function(){return this.name}").GetAwaiter().GetResult();
        public string Type => _page.CallFunctionValueAsync(ObjectId, "function(){return this.type}").GetAwaiter().GetResult();
        public long Size => long.Parse(_page.CallFunctionValueAsync(ObjectId, "function(){return this.size}").GetAwaiter().GetResult(), System.Globalization.CultureInfo.InvariantCulture);
        public long LastModified => long.Parse(_page.CallFunctionValueAsync(ObjectId, "function(){return this.lastModified}").GetAwaiter().GetResult(), System.Globalization.CultureInfo.InvariantCulture);
        public Task<string> Text() => ReadTextAsync();
        public Task<string> Base64() => ReadBase64Async();
        public Task<string> ReadTextAsync() => _page.CallFunctionValueAsync(ObjectId, "async function(){return await this.text()}");
        public Task<string> ReadBase64Async() => _page.CallFunctionValueAsync(ObjectId,
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
        public string Name { get; set; }
        public long Size { get; set; }
        public string Type { get; set; }
        public long LastModified { get; set; }
    }
}
