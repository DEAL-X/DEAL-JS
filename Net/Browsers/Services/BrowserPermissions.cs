using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Services
{
    public sealed class BrowserPermissions : BrowserService
    {
        internal BrowserPermissions(Browser browser) : base(browser) { }
        public JObject Grant<T>(string origin, T[] permissions) => Grant(origin, ToPermissions(permissions));
        public JObject Grant(string origin, IEnumerable<string> permissions) => GrantAsync(origin, permissions).GetAwaiter().GetResult();
        public Task<JObject> GrantAsync<T>(string origin, T[] permissions) => GrantAsync(origin, ToPermissions(permissions));
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
        private static IEnumerable<string> ToPermissions<T>(T[] permissions) =>
            permissions?.Select(permission => ReferenceEquals(permission, null)
                ? null
                : Convert.ToString(permission, CultureInfo.InvariantCulture));

        public JObject Reset(string origin = null) => ResetAsync(origin).GetAwaiter().GetResult();
        public Task<JObject> ResetAsync(string origin = null)
        {
            var parameters = new JObject();
            if (!string.IsNullOrWhiteSpace(origin)) parameters["origin"] = origin;
            return SendAsync("Browser.resetPermissions", parameters);
        }
    }
}
