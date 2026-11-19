using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Services
{
    public sealed class BrowserPermissions : BrowserService
    {
        internal BrowserPermissions(Browser browser) : base(browser) { }
        public JObject Grant(string origin, IEnumerable<string> permissions) => GrantAsync(origin, permissions).GetAwaiter().GetResult();

        /// <summary>Accepts JavaScript arrays marshalled as object[] by ClearScript.</summary>
        public JObject Grant(string origin, object[] permissions) =>
            GrantAsync(origin, ConvertPermissions(permissions)).GetAwaiter().GetResult();
        /// <summary>Accepts JavaScript arrays marshalled as object[] by ClearScript.</summary>
        public Task<JObject> GrantAsync(string origin, object[] permissions) =>
            GrantAsync(origin, ConvertPermissions(permissions));

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
        public JObject Reset(string origin = null) => ResetAsync(origin).GetAwaiter().GetResult();
        private static IEnumerable<string> ConvertPermissions(object[] permissions)
        {
            if (permissions == null) throw new ArgumentNullException(nameof(permissions));
            var result = new List<string>(permissions.Length);
            foreach (var permission in permissions)
                result.Add(Convert.ToString(permission, CultureInfo.InvariantCulture) ?? string.Empty);
            return result;
        }

        public Task<JObject> ResetAsync(string origin = null)
        {
            var parameters = new JObject();
            if (!string.IsNullOrWhiteSpace(origin)) parameters["origin"] = origin;
            return SendAsync("Browser.resetPermissions", parameters);
        }
    }
}
