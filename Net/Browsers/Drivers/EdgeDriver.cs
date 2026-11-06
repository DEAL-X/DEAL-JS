using System;
using System.IO;

namespace MiMFa.Net.Browsers.Drivers
{
    /// <summary>Microsoft Edge executable discovery and profile defaults.</summary>
    public sealed class EdgeDriver : ChromiumDriver
    {
        public override string Name => "Microsoft Edge";
        protected override string ProfileName => "Edge";

        public EdgeDriver(string host = null, int? port = null, string driverPath = null)
            : base(host, port, driverPath) { }

        protected override string DiscoverExecutable()
        {
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return FindExecutable(new[] { "msedge.exe", "Microsoft Edge", "microsoft-edge" },
                "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Applications", "Microsoft Edge.app", "Contents", "MacOS", "Microsoft Edge"),
                Path.Combine(programFilesX86, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(programFiles, "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(local, "Microsoft", "Edge", "Application", "msedge.exe"));
        }
    }
}
