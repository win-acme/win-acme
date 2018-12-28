using Microsoft.Win32;

namespace PKISharp.WACS.Services
{
    internal class DotNetVersionService
    {
        private ILogService _log;

        public DotNetVersionService(ILogService log)
        {
            _log = log;
        }

        public bool Check()
        {
            var release = Get45PlusFromRegistry();
            if (release < 393295)
            {
                _log.Error("This program requires .NET Framework 4.6 or higher");
                return false;
            }
             _log.Verbose(".NET Framework {x} detected", CheckFor45PlusVersion(release));
            return true;
        }

        /// <summary>
        /// From https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed#net_d
        /// </summary>
        /// <returns></returns>
        private int Get45PlusFromRegistry()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey?.GetValue("Release") != null)
                {
                    return (int)ndpKey.GetValue("Release");
                }
                else
                {
                    return -1;
                }
            }
        }

        // Checking the version using >= will enable forward compatibility.
        private string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 460798)
                return ">=4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
                return "4.6.1";
            if (releaseKey >= 393295)
                return "4.6";
            if ((releaseKey >= 379893))
                return "4.5.2";
            if ((releaseKey >= 378675))
                return "4.5.1";
            if ((releaseKey >= 378389))
                return "4.5";
            return "<4.5";
        }
    }
}