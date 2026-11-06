using MiMFa.Net.Browsers;
using MiMFa.Net.Browsers.Objects.Files;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    /// <summary>A lightweight tab handle. Its page and related objects are resolved on demand.</summary>
    public sealed class BrowserTab : IDisposable
    {
        private readonly Browser _browser;
        private BrowserPage _page;

        internal BrowserPage Page => _page ?? (_page = _browser.GetPage(Id));
        public string Id { get; }
        public string Type { get; set; }
        public object Url => Page.EvaluateValue("location.href");
        public object Title => Page.EvaluateValue("document.title");
        public string WebSocketDebuggerUrl { get; set; }
        public bool IsCurrent { get; set; }
        public BrowserWindow Window { get; }
        public BrowserWindows Windows { get; }

        internal BrowserTab(Browser browser, string id, string type = "page", string webSocketDebuggerUrl = null, bool isCurrent = false)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type;
            WebSocketDebuggerUrl = webSocketDebuggerUrl;
            IsCurrent = isCurrent;
            Window = new BrowserWindow(this);
            Windows = new BrowserWindows(this);
        }

        internal BrowserTab(Browser browser, BrowserPage page) : this(browser, page?.TargetId)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public void Dispose()
        {
            try
            {
                if (!Close()) _page?.Dispose();
            }
            catch { _page?.Dispose(); }
        }

        public string Load(string address) => LoadAsync(address).GetAwaiter().GetResult();
        public Task<string> LoadAsync(string address) => Page.ReplaceNavigateAsync(ResolveAddress(address));
        public string Navigate(string address) => NavigateAsync(address).GetAwaiter().GetResult();
        public Task<string> NavigateAsync(string address) => Page.NavigateAsync(ResolveAddress(address));
        public string Reload() => ReloadAsync().GetAwaiter().GetResult();
        public Task<string> ReloadAsync() => Page.ReloadAsync();
        public string Back() => BackAsync().GetAwaiter().GetResult();
        public Task<string> BackAsync() => Page.GoBackAsync();
        public string Forward() => ForwardAsync().GetAwaiter().GetResult();
        public Task<string> ForwardAsync() => Page.GoForwardAsync();
        public string Go(int delta) => GoAsync(delta).GetAwaiter().GetResult();
        public Task<string> GoAsync(int delta) => Page.GoAsync(delta);
        public string SetLocation(string address, bool addHistoryEntry = false) => SetLocationAsync(address, addHistoryEntry).GetAwaiter().GetResult();
        public Task<string> SetLocationAsync(string address, bool addHistoryEntry = false)
        {
            string operation = addHistoryEntry ? "pushState" : "replaceState";
            return Page.EvaluateAsync("history." + operation + "(null, null, " + Json(address == null ? null : ResolveAddress(address)) + "); location.href");
        }
        public string Locate(string address) => LocateAsync(address).GetAwaiter().GetResult();
        public Task<string> LocateAsync(string address) => SetLocationAsync(address, false);
        public string Relocate(string address) => RelocateAsync(address).GetAwaiter().GetResult();
        public Task<string> RelocateAsync(string address) => SetLocationAsync(address, true);
        public bool Close() => CloseAsync().GetAwaiter().GetResult();
        public Task<bool> CloseAsync() => _browser.CloseAsync(Id);
        public IList<BrowserFileSystemFileHandle> Open(string optionsJson = null) => OpenAsync(optionsJson).GetAwaiter().GetResult();

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

        public BrowserFileSystemFileHandle Save(string optionsJson = null) => SaveAsync(optionsJson).GetAwaiter().GetResult();

        public async Task<BrowserFileSystemFileHandle> SaveAsync(string optionsJson = null)
        {
            string objectId = await Page.EvaluateObjectAsync(
                "window.showSaveFilePicker(" + NormalizePickerOptions(optionsJson) + ")").ConfigureAwait(false);
            return new BrowserFileSystemFileHandle(Page, objectId);
        }

        internal string ResolveAddress(string address) => _browser.Router == null ? address : _browser.Router.Resolve(address, Page.Url);

        private static string NormalizePickerOptions(string optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson)) return "{}";
            var options = JObject.Parse(optionsJson);
            return options.ToString(Newtonsoft.Json.Formatting.None);
        }

        private static string Json(string value) => Newtonsoft.Json.JsonConvert.ToString(value);
    }
}
