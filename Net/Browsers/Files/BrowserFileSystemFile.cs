using MiMFa.Net.Browsers.Objects;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Files
{
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

}
