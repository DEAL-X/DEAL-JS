using MiMFa.Net.Browsers.Objects;
using System;

namespace MiMFa.Net.Browsers.Objects.Files
{
    public sealed class BrowserFileSystemFile : IDisposable
    {
        private readonly BrowserPage _page;
        private bool _disposed;

        internal string ObjectId { get; }



        public string Name => _page.CallFunctionValue(ObjectId, "function(){return this.name}");
        public string Type => _page.CallFunctionValue(ObjectId, "function(){return this.type}");
        public long Size => long.Parse(_page.CallFunctionValue(ObjectId, "function(){return this.size}"), System.Globalization.CultureInfo.InvariantCulture);
        public long LastModified => long.Parse(_page.CallFunctionValue(ObjectId, "function(){return this.lastModified}"), System.Globalization.CultureInfo.InvariantCulture);

        internal BrowserFileSystemFile(BrowserPage page, string objectId)
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

        public string Text() => ReadTextAsync().GetAwaiter().GetResult();
        public string Base64() => ReadBase64Async().GetAwaiter().GetResult();
        public string ReadText() => ReadTextAsync().GetAwaiter().GetResult();
        public System.Threading.Tasks.Task<string> ReadTextAsync() => _page.CallFunctionValueAsync(ObjectId, "async function(){return await this.text()}");
        public string ReadBase64() => ReadBase64Async().GetAwaiter().GetResult();
        public System.Threading.Tasks.Task<string> ReadBase64Async() => _page.CallFunctionValueAsync(ObjectId,
            "async function(){const b=new Uint8Array(await this.arrayBuffer());let s='';for(let i=0;i<b.length;i+=0x8000)s+=String.fromCharCode.apply(null,b.subarray(i,i+0x8000));return btoa(s)}");
    }
}
