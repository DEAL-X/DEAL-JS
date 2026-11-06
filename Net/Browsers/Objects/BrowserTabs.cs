using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MiMFa.Net.Browsers;

namespace MiMFa.Net.Browsers.Objects
{
    /// <summary>Tabs belonging to one browser session.</summary>
    public sealed class BrowserTabs
    {
        private readonly Browser _browser;


        public BrowserTab Current => _browser.Tab;
        public IList<BrowserTab> All => GetAllAsync().GetAwaiter().GetResult();
        internal BrowserTabs(Browser browser) { _browser = browser; }

        public IList<BrowserTab> GetAll() => GetAllAsync().GetAwaiter().GetResult();

        public async Task<IList<BrowserTab>> GetAllAsync()
        {
            var infos = await _browser.GetTabsAsync().ConfigureAwait(false);
            var tabs = new List<BrowserTab>();
            foreach (var info in infos)
                tabs.Add(await _browser.GetTabAsync(info.Id).ConfigureAwait(false));
            return tabs;
        }
        public BrowserTab Get(string id) => GetAsync(id).GetAwaiter().GetResult();
        public Task<BrowserTab> GetAsync(string id) => _browser.GetTabAsync(id);
        public bool Close(BrowserTab tab) => CloseAsync(tab).GetAwaiter().GetResult();
        public Task<bool> CloseAsync(BrowserTab tab) => tab == null ? Task.FromResult(false) : _browser.CloseAsync(tab.Id);
        public bool Activate(BrowserTab tab) => ActivateAsync(tab).GetAwaiter().GetResult();
        public Task<bool> ActivateAsync(BrowserTab tab) => tab == null ? Task.FromResult(false) : _browser.ActivateAsync(tab.Id);
    }
}
