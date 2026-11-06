using MiMFa.Net.Browsers.Protocols.WebDriverBiDi;
using MiMFa.Net.Browsers.Sessions;
using MiMFa.Net.Browsers;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers.Drivers
{
    /// <summary>Firefox driver using geckodriver to establish a WebDriver BiDi session.</summary>
    public sealed class FirefoxDriver : BrowserDriver
    {
        private readonly HttpClient _http = new HttpClient();
        private BiDiConnection _connection;
        private string _sessionId;
        private readonly SemaphoreSlim _sessionLock = new SemaphoreSlim(1, 1);

        public override string Name => "Mozilla Firefox";
        public string FirefoxPath { get; set; }
        public string[] Arguments { get; set; } = Array.Empty<string>();
        internal BiDiConnection Connection => _connection;
        internal string SessionId => _sessionId;

        public FirefoxDriver(string host = null, int? port = null, string driverPath = null, string firefoxPath = null)
            : base(host, port, driverPath)
        {
            FirefoxPath = firefoxPath;
        }

        public override void Dispose()
        {
            Close();
            _http.Dispose();
        }

        public override BrowserSession CreateSession(Browser browser, int? windowId = null) => new FirefoxBrowserSession(browser, this);

        public override void Initial()
        {
            if (IsAlive || (Port > 0 && IsAvailable())) return;
            if (string.IsNullOrWhiteSpace(DriverPath))
            {
                DriverPath = DiscoverExecutable();
                if (string.IsNullOrWhiteSpace(DriverPath))
                    throw new FileNotFoundException("geckodriver executable was not found.");
            }

            if (RandomizedPort || Port <= 0) Port = GetFreePort();
            string driverHost = Host;
            if (Uri.TryCreate(Host, UriKind.Absolute, out Uri hostUri)) driverHost = hostUri.Host;
            if (!IsLoopbackHost(driverHost))
                throw new InvalidOperationException("A remote geckodriver endpoint is not available at " + Endpoint + ".");

            var startInfo = new ProcessStartInfo
            {
                FileName = DriverPath,
                Arguments = "--host " + driverHost + " --port " + Port,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process = System.Diagnostics.Process.Start(startInfo);
            if (Process == null) throw new InvalidOperationException("Failed to start geckodriver.");
        }

        public override bool IsAvailable() => IsAvailableAsync().GetAwaiter().GetResult();

        public override async Task<bool> IsAvailableAsync()
        {
            try
            {
                using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/status").ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode) return false;
                    JObject status = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                    return status["value"]?["ready"]?.Value<bool>() ?? true;
                }
            }
            catch { return false; }
        }

        internal void EnsureSession() => EnsureSessionAsync().GetAwaiter().GetResult();

        internal async Task EnsureSessionAsync()
        {
            await _sessionLock.WaitAsync().ConfigureAwait(false);
            try { await EnsureSessionCoreAsync().ConfigureAwait(false); }
            finally { _sessionLock.Release(); }
        }

        private async Task EnsureSessionCoreAsync()
        {
            if (_connection != null) return;
            Initial();
            var deadline = DateTime.UtcNow.AddMilliseconds(Timeout > 0 ? Timeout : 60000);
            while (!await IsAvailableAsync().ConfigureAwait(false))
            {
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException("geckodriver did not become available.");
                await Task.Delay(100).ConfigureAwait(false);
            }

            var alwaysMatch = new JObject { ["webSocketUrl"] = true };
            if (!string.IsNullOrWhiteSpace(FirefoxPath))
                alwaysMatch["moz:firefoxOptions"] = new JObject { ["binary"] = FirefoxPath };
            if (Arguments != null && Arguments.Length > 0)
            {
                var options = alwaysMatch["moz:firefoxOptions"] as JObject ?? new JObject();
                options["args"] = new JArray(Arguments);
                alwaysMatch["moz:firefoxOptions"] = options;
            }

            var payload = new JObject
            {
                ["capabilities"] = new JObject { ["alwaysMatch"] = alwaysMatch }
            };
            using (var request = new StringContent(payload.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json"))
            using (var response = await _http.PostAsync(Endpoint.TrimEnd('/') + "/session", request).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();
                JObject result = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                JObject value = result["value"] as JObject ?? result;
                _sessionId = (string)value["sessionId"] ?? (string)result["sessionId"];
                string websocket = (string)value["capabilities"]?["webSocketUrl"];
                if (string.IsNullOrWhiteSpace(websocket) || !Uri.TryCreate(websocket, UriKind.Absolute, out Uri uri))
                {
                    if (!string.IsNullOrWhiteSpace(_sessionId))
                    {
                        try { await _http.DeleteAsync(Endpoint.TrimEnd('/') + "/session/" + Uri.EscapeDataString(_sessionId)).ConfigureAwait(false); }
                        catch { }
                    }
                    _sessionId = null;
                    throw new InvalidOperationException("Firefox did not return a valid WebDriver BiDi WebSocket URL. Ensure the installed Firefox and geckodriver support BiDi.");
                }

                var connection = new BiDiConnection();
                try
                {
                    await connection.ConnectAsync(uri, CancellationToken.None).ConfigureAwait(false);
                    _connection = connection;
                }
                catch
                {
                    connection.Dispose();
                    try
                    {
                        if (!string.IsNullOrWhiteSpace(_sessionId))
                            await _http.DeleteAsync(Endpoint.TrimEnd('/') + "/session/" + Uri.EscapeDataString(_sessionId)).ConfigureAwait(false);
                    }
                    catch { }
                    _sessionId = null;
                    throw;
                }
            }
        }

        internal Task<JObject> SendAsync(string method, JObject parameters = null)
        {
            if (_connection == null) throw new InvalidOperationException("The Firefox WebDriver BiDi session has not been initialized.");
            return _connection.SendAsync(method, parameters);
        }

        public override void Close()
        {
            _sessionLock.Wait();
            try
            {
            if (!string.IsNullOrWhiteSpace(_sessionId))
            {
                try { _http.DeleteAsync(Endpoint.TrimEnd('/') + "/session/" + Uri.EscapeDataString(_sessionId)).GetAwaiter().GetResult(); }
                catch { }
                _sessionId = null;
            }
            if (_connection != null)
            {
                _connection.Dispose();
                _connection = null;
            }
            if (Process != null)
            {
                try
                {
                    if (!Process.HasExited)
                    {
                        Process.CloseMainWindow();
                        if (!Process.WaitForExit(3000)) Process.Kill();
                        Process.WaitForExit();
                    }
                }
                finally
                {
                    Process.Dispose();
                    Process = null;
                }
            }
            }
            finally { _sessionLock.Release(); }
        }



        private static bool IsLoopbackHost(string host)
        {
            return string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase);
        }

        private static string DiscoverExecutable()
        {
            string[] names = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? new[] { "geckodriver.exe" }
                : new[] { "geckodriver" };
            string pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrWhiteSpace(pathVariable))
            {
                foreach (string directory in pathVariable.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(directory)) continue;
                    foreach (string name in names)
                    {
                        string path = Path.Combine(directory.Trim(), name);
                        if (File.Exists(path)) return Path.GetFullPath(path);
                    }
                }
            }
            return null;
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
            finally { listener.Stop(); }
        }
    }
}
