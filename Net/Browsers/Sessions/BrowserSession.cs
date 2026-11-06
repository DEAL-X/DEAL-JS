using MiMFa.Net.Browsers.Objects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Sessions
{
    /// <summary>Protocol-neutral operations for one browser window/session.</summary>
    public abstract class BrowserSession : IDisposable
    {
        public abstract int? WindowId { get; }
        public abstract BrowserPage CurrentPage { get; protected set; }
        public abstract BrowserTab Tab { get; }

        public abstract void Dispose();
        public abstract void Initial();
        public abstract bool IsAvailable();
        public abstract Task<bool> IsAvailableAsync();
        public abstract void CloseBrowser();

        public abstract BrowserTabs GetTabs();
        public abstract Task<BrowserTabs> GetTabsAsync();
        public abstract BrowserPage GetPage(string targetId);
        public abstract Task<BrowserPage> GetPageAsync(string targetId);
        public abstract BrowserTab GetTab(string targetId);
        public abstract Task<BrowserTab> GetTabAsync(string targetId);
        public abstract BrowserTab OpenTab(string url = null, int timeoutMilliseconds = 0);
        public abstract Task<BrowserTab> OpenTabAsync(string url = null, int timeoutMilliseconds = 0);
        public abstract BrowserPage OpenPage(string url = null, int timeoutMilliseconds = 0);
        public abstract Task<BrowserPage> OpenAsync(string url = null, int timeoutMilliseconds = 0);
        public abstract string Go(string address, int timeoutMilliseconds = 0);
        public abstract Task<string> GoAsync(string address, int timeoutMilliseconds = 0);
        public abstract string Reload();
        public abstract Task<string> ReloadAsync();
        public abstract string SetLocation(string address, bool addHistoryEntry = false);
        public abstract Task<string> SetLocationAsync(string address, bool addHistoryEntry = false);
        public abstract string GoBack();
        public abstract Task<string> GoBackAsync();
        public abstract string GoForward();
        public abstract Task<string> GoForwardAsync();
        public abstract BrowserPage Load(string url, int timeoutMilliseconds = -1);
        public abstract Task<BrowserPage> LoadAsync(string url, int timeoutMilliseconds = -1);
        public abstract bool Activate(string targetId);
        public abstract Task<bool> ActivateAsync(string targetId);
        public abstract bool Close(string targetId);
        public abstract Task<bool> CloseAsync(string targetId);
    }
}
