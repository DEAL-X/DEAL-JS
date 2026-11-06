using System;
using System.IO;

namespace MiMFa.Net.Browsers.Drivers
{
    /// <summary>Chrome-specific executable discovery and profile defaults.</summary>
    public sealed class ChromeDriver : ChromiumDriver
    {
        public override string Name => "Google Chrome";
        protected override string ProfileName => "Chrome";

        public ChromeDriver(string host = null, int? port = null, string driverPath = null)
            : base(host, port, driverPath) { }

        protected override string DiscoverExecutable()
        {
            string programFiles = Path.Combine(Path.GetPathRoot( Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)), "Program Files");
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return FindExecutable(new[] { "chrome.exe", "chrome", "google-chrome", "chromium", "chromium-browser" },
                "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "Google Chrome.app", "Contents", "MacOS", "Google Chrome"),
                Path.Combine(programFilesX86, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(programFiles, "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(local, "Google", "Chrome", "Application", "chrome.exe"));
        }
    }
}
