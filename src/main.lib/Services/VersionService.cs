using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PKISharp.WACS.Services
{
    public class VersionService
    {
        public VersionService(ILogService log)
        {
            var processInfo = new FileInfo(Process.GetCurrentProcess().MainModule.FileName);
            if (processInfo.Name == "dotnet.exe")
            {
                log.Error("Running as a local dotnet tool is not supported. Please install using the --global option.");
                throw new InvalidOperationException();
            }

            ExePath = processInfo.FullName;
            AssemblyPath = processInfo.DirectoryName;
            ResourcePath = processInfo.DirectoryName;
            if (!processInfo.Directory.GetFiles("settings*.json").Any())
            {
                AssemblyPath = "";
                var entryAssemblyInfo = new FileInfo(Assembly.GetEntryAssembly().Location);
                ResourcePath = entryAssemblyInfo.DirectoryName;
            }

            log.Verbose("ExePath: {ex}", ExePath);
            log.Verbose("ResourcePath: {ex}", ResourcePath);
        }

        public string AssemblyPath { get; private set; } = "";
        public string ExePath { get; private set; } = "";
        public string ResourcePath { get; private set; } = "";
        public string Bitness => Environment.Is64BitProcess ? "64-bit" : "32-bit";
        public bool Pluggable =>
#if PLUGGABLE
                true;
#else
                false;
#endif
        public bool Debug =>
#if DEBUG
                true;
#else
                false;
#endif

        public string BuildType 
        { 
            get
            {
                var build = $"{(Debug ? "DEBUG" : "RELEASE")}, {(Pluggable ? "PLUGGABLE" : "TRIMMED")}";
                return build;
            }
        }

        public Version SoftwareVersion => Assembly.GetEntryAssembly().GetName().Version;
    }
}
