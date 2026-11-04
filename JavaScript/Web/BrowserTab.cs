using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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

        public Task<string> ReloadAsync() => Page.ReloadAsync();
        public Task<bool> CloseAsync() => _browser.CloseAsync(Id);
        public Task<bool> ActivateAsync() => _browser.ActivateAsync(Id);

        // Direct aliases for the native File System Access API. The picker and
        // its security/permission checks remain entirely owned by Chromium.
        private static string NormalizePickerOptions(string optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson)) return "{}";
            var options = JObject.Parse(optionsJson);
            return options.ToString(Newtonsoft.Json.Formatting.None);
        }

        public async Task<IList<BrowserFileSystemFileHandle>> OpenAsync(string optionsJson = null)
        {
            string objectId = await Page.EvaluateObjectAsync(
                "window.showOpenFilePicker(" + NormalizePickerOptions(optionsJson) + ")").ConfigureAwait(false);
            try
            {
                string lengthText = await Page.CallFunctionValueAsync(objectId, "function(){return this.length}").ConfigureAwait(false);
                int length = int.Parse(lengthText, System.Globalization.CultureInfo.InvariantCulture);
                var handles = new List<BrowserFileSystemFileHandle>(length);
                for (int i = 0; i < length; i++)
                {
                    string handleId = await Page.CallFunctionObjectAsync(objectId,
                        "function(index){return this[index]}",
                        new JArray { new JObject { ["value"] = i } }).ConfigureAwait(false);
                    handles.Add(new BrowserFileSystemFileHandle(Page, handleId));
                }
                return handles;
            }
            finally { await Page.ReleaseObjectAsync(objectId).ConfigureAwait(false); }
        }

        public async Task<BrowserFileSystemFileHandle> SaveAsync(string optionsJson = null)
        {
            string objectId = await Page.EvaluateObjectAsync(
                "window.showSaveFilePicker(" + NormalizePickerOptions(optionsJson) + ")").ConfigureAwait(false);
            return new BrowserFileSystemFileHandle(Page, objectId);
        }

        public Task<IList<BrowserFileSystemFileHandle>> open(string optionsJson = null) => OpenAsync(optionsJson);
        public Task<BrowserFileSystemFileHandle> save(string optionsJson = null) => SaveAsync(optionsJson);

        public void Dispose()
        {
            try { CloseAsync().GetAwaiter().GetResult(); }
            catch { Page.Dispose(); }
        }
    }
}
