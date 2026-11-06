using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Files
{
    public sealed class BrowserFileMetadata
    {
        public string Name { get; set; }
        public long Size { get; set; }
        public string Type { get; set; }
        public long LastModified { get; set; }
    }
}
