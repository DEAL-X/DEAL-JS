using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

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
        public BrowserWindow window => Window;
        public BrowserWindows windows => Windows;
        public BrowserLocation location => Location;
        public Task<string> reload() => ReloadAsync();
        public Task<bool> close() => CloseAsync();
        public Task<bool> activate() => ActivateAsync();
        public Task<string> open(string optionsJson = null) => OpenAsync(optionsJson);
        public Task<string> save(string optionsJson = null) => SaveAsync(optionsJson);

        public Task<string> ReloadAsync() => Page.ReloadAsync();
        public Task<bool> CloseAsync() => _browser.CloseAsync(Id);
        public Task<bool> ActivateAsync() => _browser.ActivateAsync(Id);

        // Native File System Access API aliases. Chromium owns the picker UI.
        private static string NormalizePickerOptions(string optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson)) return "{}";
            var options = JObject.Parse(optionsJson);
            return options.ToString(Newtonsoft.Json.Formatting.None);
        }

        public Task<string> OpenAsync(string optionsJson = null) =>
            Page.EvaluateAsync("window.showOpenFilePicker(" + NormalizePickerOptions(optionsJson) + ").then(handles=>JSON.stringify(handles.map(h=>({name:h.name,kind:h.kind}))))");
        public Task<string> SaveAsync(string optionsJson = null) =>
            Page.EvaluateAsync("window.showSaveFilePicker(" + NormalizePickerOptions(optionsJson) + ").then(handle=>JSON.stringify({name:handle.name,kind:handle.kind}))");

        public void Dispose() => Page.Dispose();
    }
}
