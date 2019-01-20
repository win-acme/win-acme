using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Extensions
{
    public static class MainArgumentsExtensions
    {
        /// <summary>
        /// Get BaseUri to use  
        /// </summary>
        /// <param name="options"></param>
        public static string GetBaseUri(this MainArguments options, bool import = false)
        {
            if (import)
            {
                return options.ImportBaseUri ?? Properties.Settings.Default.DefaultBaseUriImport;
            }
            else if (options.Test)
            {
                return options.BaseUri ?? Properties.Settings.Default.DefaultBaseUriTest;
            }
            else
            {
                return options.BaseUri ?? Properties.Settings.Default.DefaultBaseUri;
            }
        }

        /// <summary>
        /// Reset the options for a(nother) run through the main menu
        /// </summary>
        /// <param name="options"></param>
        public static void Clear(this MainArguments options)
        {
            options.Target = null;
            options.Renew = false;
            options.FriendlyName = null;
            options.Force = false;
            options.List = false;
            options.Version = false;
            options.Help = false;
        }
    }
}
