using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Objects
{
    /// <summary>Tabs belonging to one application-level Chromium browser instance.</summary>
    public sealed class BrowserTabs
    {
        private readonly Browser _browser;
        internal BrowserTabs(Browser browser) { _browser = browser; }

        public BrowserTab Current => _browser.Tab;
        public IList<BrowserTab> All => GetAllAsync().GetAwaiter().GetResult();
        public BrowserTab Get(string id) => GetAsync(id).GetAwaiter().GetResult();
        public bool Close(BrowserTab tab) => CloseAsync(tab).GetAwaiter().GetResult();
        public bool Activate(BrowserTab tab) => ActivateAsync(tab).GetAwaiter().GetResult();
        public async Task<IList<BrowserTab>> GetAllAsync()
        {
            var infos = await _browser.GetTabsAsync().ConfigureAwait(false);
            var tabs = new List<BrowserTab>();
            foreach (var info in infos)
                tabs.Add(await _browser.GetTabAsync(info.Id).ConfigureAwait(false));
            return tabs;
        }
        public Task<BrowserTab> GetAsync(string id) => _browser.GetTabAsync(id);
        public Task<bool> CloseAsync(BrowserTab tab) => tab == null ? Task.FromResult(false) : _browser.CloseAsync(tab.Id);
        public Task<bool> ActivateAsync(BrowserTab tab) => tab == null ? Task.FromResult(false) : _browser.ActivateAsync(tab.Id);
    }
}
