using System;
using System.Diagnostics;
using System.Threading.Tasks;
using MiMFa.Net.Browsers;
using MiMFa.Net.Browsers.Sessions;

namespace MiMFa.Net.Browsers.Drivers
{
    /// <summary>Defines the lifecycle and endpoint contract shared by browser drivers.</summary>
    public abstract class BrowserDriver : IDisposable
    {
        public abstract string Name { get; }
        public Process Process { get; set; }
        public string DriverPath { get; set; }
        public string Host { get; set; } = "http://127.0.0.1";
        public int Port { get; set; }
        public bool RandomizedPort { get; set; }
        public int Timeout { get; set; } = 60000;
        public string Endpoint => Host.TrimEnd('/') + ":" + Port;
        public bool IsAlive => Process != null && !Process.HasExited;

        protected BrowserDriver(string host = null, int? port = null, string driverPath = null)
        {
            Host = host ?? "http://127.0.0.1";
            Port = port ?? 0;
            DriverPath = driverPath;
            RandomizedPort = !port.HasValue;
        }

        public virtual void Dispose()
        {
            Close();
        }

        public abstract BrowserSession CreateSession(Browser browser, int? windowId = null);
        public abstract void Initial();
        public abstract bool IsAvailable();
        public abstract Task<bool> IsAvailableAsync();
        public abstract void Close();


    }
}
