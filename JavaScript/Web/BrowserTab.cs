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
        internal string ResolveAddress(string address) => _browser.PathProvider == null ? address : _browser.PathProvider.Resolve(address, Page.Url);
        private static string Json(string value) => Newtonsoft.Json.JsonConvert.ToString(value);
        public string Id => Page.TargetId;
        public object Url => Page.EvaluateValueAsync("location.href").GetAwaiter().GetResult();
        public object Title => Page.EvaluateValueAsync("document.title").GetAwaiter().GetResult();
        public BrowserWindow Window { get; }
        public BrowserWindows Windows { get; }
        // Navigation and history operations are scoped to the current tab.

        public string Load(string address) => LoadAsync(address).GetAwaiter().GetResult();
        public string Navigate(string address) => NavigateAsync(address).GetAwaiter().GetResult();
        public string Reload() => ReloadAsync().GetAwaiter().GetResult();
        public string Back() => Page.GoBackAsync().GetAwaiter().GetResult();
        public string Forward() => Page.GoForwardAsync().GetAwaiter().GetResult();
        public string Go(int delta) => Page.GoAsync(delta).GetAwaiter().GetResult();
        public string Locate(string address) => SetLocationAsync(address, false).GetAwaiter().GetResult();
        public string Relocate(string address) => SetLocationAsync(address, true).GetAwaiter().GetResult();
        public bool Close() => CloseAsync().GetAwaiter().GetResult();
        public IList<BrowserFileSystemFileHandle> Open(string optionsJson = null) => OpenAsync(optionsJson).GetAwaiter().GetResult();
        public BrowserFileSystemFileHandle Save(string optionsJson = null) => SaveAsync(optionsJson).GetAwaiter().GetResult();

        public Task<string> LoadAsync(string address) => Page.ReplaceNavigateAsync(ResolveAddress(address));
        public Task<string> NavigateAsync(string address) => Page.NavigateAsync(ResolveAddress(address));
        public Task<string> SetLocationAsync(string address, bool addHistoryEntry)
        {
            string operation = addHistoryEntry ? "pushState" : "replaceState";
            return Page.EvaluateAsync("history." + operation + "(null, null, " + Json(address == null ? null : ResolveAddress(address)) + "); location.href");
        }

        public Task<string> ReloadAsync() => Page.ReloadAsync();
        public Task<bool> CloseAsync() => _browser.CloseAsync(Id);

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


        public void Dispose()
        {
            try
            {
                if (!CloseAsync().GetAwaiter().GetResult()) Page.Dispose();
            }
            catch { Page.Dispose(); }
        }
    }
}
