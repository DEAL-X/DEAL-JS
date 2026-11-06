using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using MiMFa.Net.Browsers;
using MiMFa.Net.Browsers.Sessions;

namespace MiMFa.Net.Browsers.Drivers
{
    /// <summary>Shared process, profile, and debugging-endpoint implementation for Chromium browsers.</summary>
    public abstract class ChromiumDriver : BrowserDriver
    {
        private readonly HttpClient _http = new HttpClient();
        protected abstract string ProfileName { get; }

        protected ChromiumDriver(string host = null, int? port = null, string driverPath = null)
            : base(host, port, driverPath) { }

        protected abstract string DiscoverExecutable();

        public override void Dispose()
        {
            Close();
            _http.Dispose();
        }

        public override BrowserSession CreateSession(Browser browser, int? windowId = null) => new ChromiumBrowserSession(browser, this, windowId);

        public override void Initial()
        {
            if (IsAlive) return;
            if (Port > 0 && IsAvailable()) return;

            if (string.IsNullOrWhiteSpace(DriverPath))
            {
                DriverPath = DiscoverExecutable();
                if (string.IsNullOrWhiteSpace(DriverPath) || !File.Exists(DriverPath))
                    throw new FileNotFoundException(Name + " executable was not found.", DriverPath);
            }

            string profilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Drivers", ProfileName, "Profile");
            Directory.CreateDirectory(profilePath);
            if (RandomizedPort || Port <= 0) Port = GetFreePort();

            var startInfo = new ProcessStartInfo
            {
                FileName = DriverPath,
                Arguments = $"--remote-debugging-port={Port} --user-data-dir=\"{profilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = false
            };
            Process = System.Diagnostics.Process.Start(startInfo);
            if (Process == null) throw new InvalidOperationException("Failed to start " + Name + ".");
        }

        public override bool IsAvailable() => IsAvailableAsync().GetAwaiter().GetResult();

        public override async Task<bool> IsAvailableAsync()
        {
            try
            {
                using (var response = await _http.GetAsync(Endpoint.TrimEnd('/') + "/json/version").ConfigureAwait(false))
                    return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public override void Close()
        {
            if (Process == null) return;
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



        protected static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try { return ((IPEndPoint)listener.LocalEndpoint).Port; }
            finally { listener.Stop(); }
        }

        protected static string FindExecutable(string[] executableNames, params string[] paths)
        {
            foreach (string path in paths)
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    return Path.GetFullPath(path);

            string pathVariable = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrWhiteSpace(pathVariable))
            {
                foreach (string directory in pathVariable.Split(Path.PathSeparator))
                {
                    if (string.IsNullOrWhiteSpace(directory)) continue;
                    foreach (string candidate in executableNames)
                    {
                        string fullPath = Path.Combine(directory.Trim(), candidate);
                        if (File.Exists(fullPath)) return Path.GetFullPath(fullPath);
                    }
                }
            }
            return null;
        }

    }
}
