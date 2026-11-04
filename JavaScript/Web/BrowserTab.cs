using System;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>A tab handle. Its Window and related objects are remote Chromium proxies.</summary>
    public sealed class BrowserTab : IDisposable
    {
        private readonly BrowserProvider _browser;
        internal BrowserTab(BrowserProvider browser, BrowserPage page)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Window = new BrowserWindow(this);
            Windows = new BrowserWindows(this);
        }

        internal BrowserPage Page { get; }
        public string Id => Page.TargetId;
        public string Url => Page.Url;
        public string Title => Page.Title;
        public BrowserWindow Window { get; }
        public BrowserWindows Windows { get; }
        public BrowserLocation Location => Window.Location;

        public Task<string> ReloadAsync() => Page.ReloadAsync();
        public Task<bool> CloseAsync() => _browser.CloseAsync(Id);
        public Task<bool> ActivateAsync() => _browser.ActivateAsync(Id);

        // Aliases of the native File System Access API. Chromium owns the picker UI.
        public Task<string> OpenAsync() => Page.EvaluateAsync("window.showOpenFilePicker().then(handles=>JSON.stringify(handles.map(h=>({name:h.name,kind:h.kind}))))");
        public Task<string> SaveAsync() => Page.EvaluateAsync("window.showSaveFilePicker().then(handle=>JSON.stringify({name:handle.name,kind:handle.kind}))");

        public void Dispose() => Page.Dispose();
    }
}
