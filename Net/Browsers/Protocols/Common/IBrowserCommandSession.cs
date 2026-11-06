using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Protocols.Common
{
    /// <summary>Optional browser-level protocol command capability.</summary>
    public interface IBrowserCommandSession
    {
        JObject SendBrowserCommand(string method, JObject parameters = null);
        Task<JObject> SendBrowserCommandAsync(string method, JObject parameters = null);
    }
}
