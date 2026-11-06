using MiMFa.Net;
using MiMFa.Net.Browsers.Drivers;
using MiMFa.Net.Browsers.Objects;
using MiMFa.Net.Browsers.Services;
using MiMFa.Net.Browsers.Sessions;
using MiMFa.Net.Browsers.Protocols.Common;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MiMFa.Net.Browsers
{
    /// <summary>Browser-independent provider facade over a selected browser driver and session.</summary>
    public sealed class Browser : IDisposable
    {
        public BrowserDriver Driver { get; }
        public BrowserSession Session { get; }
        public BrowserDownloads Downloads { get; }
        public BrowserPermissions Permissions { get; }
        public BrowserRuntime Runtime { get; }
        public Router Router { get; set; }
        public Process Process { get => Driver.Process; set => Driver.Process = value; }
        public string DriverPath { get => Driver.DriverPath; set => Driver.DriverPath = value; }
        public string Host { get => Driver.Host; set => Driver.Host = value; }
        public int Port { get => Driver.Port; set => Driver.Port = value; }
        public bool RandomizedPort { get => Driver.RandomizedPort; set => Driver.RandomizedPort = value; }
        public string Endpoint => Driver.Endpoint;
        public int Timeout { get => Driver.Timeout; set => Driver.Timeout = value; }
        public bool IsAlive => Driver.IsAlive;
        public int? WindowId => Session.WindowId;
        public BrowserPage CurrentPage => Session.CurrentPage;
        public BrowserTab Tab => Session.Tab;
        public BrowserTabs Tabs => GetTabs();

        public Browser(string host = null, int? port = null, string driverPath = null, int? windowId = null, BrowserDriver driver = null)
        {
            Driver = driver ?? new ChromeDriver(host, port, driverPath);
            Session = Driver.CreateSession(this, windowId);
            Downloads = new BrowserDownloads(this);
            Permissions = new BrowserPermissions(this);
            Runtime = new BrowserRuntime(this);
        }



        public void Dispose()
        {
            Session.Dispose();
            Driver.Dispose();
        }

        public void Initial() => Session.Initial();
        public void CloseBrowser() => Session.CloseBrowser();
        public void Close() => CloseBrowser();
        public void Final() => Dispose();

        public bool IsAvailable() => Session.IsAvailable();
        public Task<bool> IsAvailableAsync() => Session.IsAvailableAsync();

        public bool Activate(BrowserTab tab) => tab != null && Session.Activate(tab.Id);
        public bool Activate(string targetId) => Session.Activate(targetId);
        public Task<bool> ActivateAsync(string targetId) => Session.ActivateAsync(targetId);
        public bool Close(string targetId) => Session.Close(targetId);
        public Task<bool> CloseAsync(string targetId) => Session.CloseAsync(targetId);

        public BrowserTabs GetTabs() => Session.GetTabs();
        public Task<BrowserTabs> GetTabsAsync() => Session.GetTabsAsync();
        public BrowserPage GetPage(string targetId) => Session.GetPage(targetId);
        public Task<BrowserPage> GetPageAsync(string targetId) => Session.GetPageAsync(targetId);
        public BrowserTab GetTab(string targetId) => Session.GetTab(targetId);
        public Task<BrowserTab> GetTabAsync(string targetId) => Session.GetTabAsync(targetId);
        public BrowserTab OpenTab(string url = null, int timeoutMilliseconds = 0) => Session.OpenTab(url, timeoutMilliseconds);
        public Task<BrowserTab> OpenTabAsync(string url = null, int timeoutMilliseconds = 0) => Session.OpenTabAsync(url, timeoutMilliseconds);
        public BrowserPage OpenPage(string url = null, int timeoutMilliseconds = 0) => Session.OpenPage(url, timeoutMilliseconds);
       
        public BrowserTab Open(string url = null, int timeoutMilliseconds = 0) => Session.OpenTab(url ?? "about:blank", timeoutMilliseconds);
        public Task<BrowserPage> OpenAsync(string url = null, int timeoutMilliseconds = 0) => Session.OpenAsync(url, timeoutMilliseconds);

        public BrowserPage Load(string url, int timeoutMilliseconds = 0) => Session.Load(url, timeoutMilliseconds);
        public Task<BrowserPage> LoadAsync(string url, int timeoutMilliseconds = 0) => Session.LoadAsync(url, timeoutMilliseconds);
        public string Reload() => Session.Reload();
        public Task<string> ReloadAsync() => Session.ReloadAsync();

        public string Go(string address, int timeoutMilliseconds = 0) => Session.Go(address, timeoutMilliseconds);
        public Task<string> GoAsync(string address, int timeoutMilliseconds = 0) => Session.GoAsync(address, timeoutMilliseconds);
        public string GoBack() => Session.GoBack();
        public Task<string> GoBackAsync() => Session.GoBackAsync();
        public string GoForward() => Session.GoForward();
        public Task<string> GoForwardAsync() => Session.GoForwardAsync();

        public string SetLocation(string address, bool addHistoryEntry = false) => Session.SetLocation(address, addHistoryEntry);
        public Task<string> SetLocationAsync(string address, bool addHistoryEntry = false) => Session.SetLocationAsync(address, addHistoryEntry);

        internal JObject SendBrowserCommand(string method, JObject parameters = null)
        {
            if (Session is IBrowserCommandSession commandSession)
                return commandSession.SendBrowserCommand(method, parameters);
            throw new NotSupportedException("The selected browser driver does not support browser-level protocol commands.");
        }
        internal Task<JObject> SendBrowserCommandAsync(string method, JObject parameters = null)
        {
            if (Session is IBrowserCommandSession commandSession)
                return commandSession.SendBrowserCommandAsync(method, parameters);
            throw new NotSupportedException("The selected browser driver does not support browser-level protocol commands.");
        }
    }
}
