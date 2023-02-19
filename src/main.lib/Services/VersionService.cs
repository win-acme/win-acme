using Serilog;
using System;
#if !DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Services
{
    public class VersionService
    {
        private readonly ILogService _log;

        public VersionService(ILogService log) => _log = log;

        public bool Init()
        {
            if (ExePath == null)
            {
                _log.Error("Unable to determine main module filename.");
                return false;
            }
            var processInfo = new FileInfo(ExePath);

            // Check for running as local .NET tool
            if (processInfo.Name == "dotnet.exe")
            {
                _log.Error("Running as a local dotnet tool is not supported. Please install using the --global option.");
                return false;
            }
            // Check for running as global .NET tool
            if (processInfo.Name == "wacs.dll")
            {
#if !DEBUG
                DotNetTool = true;
                PluginPath = processInfo.DirectoryName!;
                processInfo = new FileInfo(Process.GetCurrentProcess().MainModule?.FileName!);
                ExePath = processInfo.FullName;
                SettingsPath = Path.Combine(processInfo.Directory!.FullName, ".store", "win-acme");
#endif
            }
            _log.Verbose("ExePath: {ex}", ExePath);
            _log.Verbose("ResourcePath: {ex}", ResourcePath);
            _log.Verbose("PluginPath: {ex}", PluginPath);
            return true;
        }

        public static bool DotNetTool { get; private set; } = false;
        public static string SettingsPath { get; private set; } = AppContext.BaseDirectory;
        public static string BasePath { get; private set; } = AppContext.BaseDirectory;
        public static string PluginPath { get; private set; } = AppContext.BaseDirectory;
        public static string ExePath { get; private set; } = Environment.GetCommandLineArgs().First();
        public static string ResourcePath { get; private set; } = AppContext.BaseDirectory;
        public static string Bitness => Environment.Is64BitProcess ? "64-bit" : "32-bit";
        public static bool Pluggable =>
#if PLUGGABLE
                true;
#else
                false;
#endif
        public static bool Debug =>
#if DEBUG
                true;
#else
                false;
#endif

        public static string BuildType 
        { 
            get
            {
                var build = $"{(Debug ? "debug" : "release")}, " +
                    $"{(Pluggable ? "pluggable" : "trimmed")}, " +
                    $"{(DotNetTool ? "dotnet" : "standalone")}";
                return build;
            }
        }

        public static Version SoftwareVersion => 
            Assembly.GetEntryAssembly()?.GetName().Version ?? 
            new Version("2.2.0.0");
    }
}
