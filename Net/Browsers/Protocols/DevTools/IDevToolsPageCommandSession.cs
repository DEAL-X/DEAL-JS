using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Protocols.DevTools
{
    /// <summary>Optional raw-command access for a DevTools-backed page session.</summary>
    internal interface IDevToolsPageCommandSession
    {
        JObject SendCommand(string method, JObject parameters = null);
        Task<JObject> SendCommandAsync(string method, JObject parameters = null);
    }
}
