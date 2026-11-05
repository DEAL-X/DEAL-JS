using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Engine.JavaScript.Web
{
    /// <summary>Tabs belonging to one application-level Chromium browser instance.</summary>
    public sealed class BrowserTabs
    {
        private readonly BrowserProvider _browser;
        internal BrowserTabs(BrowserProvider browser) { _browser = browser; }

        public BrowserTab Current => _browser.Tab;
        public BrowserTab current => Current;
        public IList<BrowserTab> all => GetAllAsync().GetAwaiter().GetResult();
        public Task<BrowserTab> get(string id) => GetAsync(id);
        public Task<bool> close(BrowserTab tab) => CloseAsync(tab);
        public Task<bool> activate(BrowserTab tab) => ActivateAsync(tab);
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
