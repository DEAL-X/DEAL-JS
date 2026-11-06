using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Services
{
    public sealed class BrowserPermissions : BrowserService
    {
        internal BrowserPermissions(Browser browser) : base(browser) { }
        public JObject Grant(string origin, IEnumerable<string> permissions) => GrantAsync(origin, permissions).GetAwaiter().GetResult();
        public JObject Reset(string origin = null) => ResetAsync(origin).GetAwaiter().GetResult();
        public Task<JObject> GrantAsync(string origin, IEnumerable<string> permissions)
        {
            if (string.IsNullOrWhiteSpace(origin)) throw new ArgumentException("An origin is required.", nameof(origin));
            if (permissions == null) throw new ArgumentNullException(nameof(permissions));
            return SendAsync("Browser.grantPermissions", new JObject
            {
                ["origin"] = origin,
                ["permissions"] = new JArray(permissions)
            });
        }
        public Task<JObject> ResetAsync(string origin = null)
        {
            var parameters = new JObject();
            if (!string.IsNullOrWhiteSpace(origin)) parameters["origin"] = origin;
            return SendAsync("Browser.resetPermissions", parameters);
        }
    }
}
