using Newtonsoft.Json.Linq;

namespace MiMFa.Net.Fetch
{
    /// <summary>Describes one Fetch-compatible HTTP or local-file request.</summary>
    public sealed class FetchRequest
    {
        public string Method { get; set; } = "GET";
        public string Url { get; set; }
        public string Data { get; set; }
        public string ContentType { get; set; } = "application/json";
        public int Timeout { get; set; } = 30000;
        public JObject Headers { get; set; }
        public string Id { get; set; }
        public string BodyBase64 { get; set; }
        public string Credentials { get; set; } = "same-origin";
    }
}
