using System;
using System.Reflection;

namespace PKISharp.WACS.Services
{
    public class VersionService
    {
        public string Bitness => Environment.Is64BitProcess ? "64-bit" : "32-bit";

        public string BuildType 
        { 
            get
            {
                var build = "";
#if DEBUG
                    build += "DEBUG";
#else
                    build += "RELEASE";
#endif
#if PLUGGABLE
                    build += ", PLUGGABLE";
#else
                    build += ", TRIMMED";
#endif
                return build;
            }
        }

        public Version SoftwareVersion => Assembly.GetEntryAssembly().GetName().Version;
    }
}
