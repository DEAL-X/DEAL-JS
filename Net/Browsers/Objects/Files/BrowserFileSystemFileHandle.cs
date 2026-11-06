using MiMFa.Net.Browsers.Objects;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects.Files
{
    public sealed class BrowserFileSystemFileHandle : IDisposable
    {
        private readonly BrowserPage _page;
        private bool _disposed;

        internal string ObjectId { get; }
        public string Name => GetName();
        public string Kind => GetKind();

        internal BrowserFileSystemFileHandle(BrowserPage page, string objectId)
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

        public string GetName() => GetNameAsync().GetAwaiter().GetResult();
        public Task<string> GetNameAsync() => _page.CallFunctionValueAsync(ObjectId, "function(){return this.name}");
        public string GetKind() => GetKindAsync().GetAwaiter().GetResult();
        public Task<string> GetKindAsync() => _page.CallFunctionValueAsync(ObjectId, "function(){return this.kind}");
        public BrowserFileSystemFile GetFile() => GetFileAsync().GetAwaiter().GetResult();
        public async Task<BrowserFileSystemFile> GetFileAsync()
        {
            string objectId = await _page.CallFunctionObjectAsync(ObjectId, "async function(){return await this.getFile()}").ConfigureAwait(false);
            return new BrowserFileSystemFile(_page, objectId);
        }
        public string Text() => ReadTextAsync().GetAwaiter().GetResult();
        public string ReadText() => ReadTextAsync().GetAwaiter().GetResult();
        public Task<string> ReadTextAsync() =>
            _page.CallFunctionValueAsync(ObjectId, "async function(){return await (await this.getFile()).text()}");
        public string Base64() => ReadBase64Async().GetAwaiter().GetResult();
        public string ReadBase64() => ReadBase64Async().GetAwaiter().GetResult();
        public Task<string> ReadBase64Async() =>
            _page.CallFunctionValueAsync(ObjectId,
                "async function(){const b=new Uint8Array(await (await this.getFile()).arrayBuffer());let s='';for(let i=0;i<b.length;i+=0x8000)s+=String.fromCharCode.apply(null,b.subarray(i,i+0x8000));return btoa(s)}");
        public bool WriteText(string text) => WriteTextAsync(text).GetAwaiter().GetResult();
        public async Task<bool> WriteTextAsync(string text)
        {
            string result = await _page.CallFunctionValueAsync(ObjectId,
                "async function(value){const w=await this.createWritable();try{await w.write(value);await w.close();return true}catch(e){try{await w.abort()}catch(_){}throw e}}",
                new JArray { new JObject { ["value"] = text ?? string.Empty } }).ConfigureAwait(false);
            return bool.TryParse(result, out bool success) && success;
        }
        public BrowserFileSystemWritable CreateWritable() => CreateWritableAsync().GetAwaiter().GetResult();
        public async Task<BrowserFileSystemWritable> CreateWritableAsync()
        {
            string objectId = await _page.CallFunctionObjectAsync(ObjectId, "async function(){return await this.createWritable()}").ConfigureAwait(false);
            return new BrowserFileSystemWritable(_page, objectId);
        }
        public string QueryPermission(string mode = "read") => QueryPermissionAsync(mode).GetAwaiter().GetResult();
        public Task<string> QueryPermissionAsync(string mode = "read") => PermissionAsync("queryPermission", mode);
        public string RequestPermission(string mode = "readwrite") => RequestPermissionAsync(mode).GetAwaiter().GetResult();
        public Task<string> RequestPermissionAsync(string mode = "readwrite") => PermissionAsync("requestPermission", mode);
        public bool IsSameEntry(BrowserFileSystemFileHandle other) => IsSameEntryAsync(other).GetAwaiter().GetResult();
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

        private Task<string> PermissionAsync(string method, string mode) =>
            _page.CallFunctionValueAsync(ObjectId,
                "async function(value){return await this[" + Newtonsoft.Json.JsonConvert.SerializeObject(method) + "]({mode:value})}",
                new JArray { new JObject { ["value"] = mode ?? "read" } });
    }
}
