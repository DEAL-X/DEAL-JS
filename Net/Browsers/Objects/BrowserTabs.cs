using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    /// <summary>Tab handles belonging to one browser session.</summary>
    public sealed class BrowserTabs : List<BrowserTab>
    {
        private readonly Browser _browser;

        public BrowserTab Current => _browser.Tab;

        internal BrowserTabs(Browser browser, IEnumerable<BrowserTab> tabs = null) : base(tabs ?? new List<BrowserTab>())
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        }

        public BrowserTab Get(string id) => GetAsync(id).GetAwaiter().GetResult();
        public Task<BrowserTab> GetAsync(string id) => _browser.GetTabAsync(id);
        public bool Close(BrowserTab tab) => CloseAsync(tab).GetAwaiter().GetResult();
        public Task<bool> CloseAsync(BrowserTab tab) => tab == null ? Task.FromResult(false) : _browser.CloseAsync(tab.Id);
        public bool Activate(BrowserTab tab) => ActivateAsync(tab).GetAwaiter().GetResult();
        public Task<bool> ActivateAsync(BrowserTab tab) => tab == null ? Task.FromResult(false) : _browser.ActivateAsync(tab.Id);
    }
}