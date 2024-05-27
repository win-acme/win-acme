using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.ValidationPlugins;
namespace PKISharp.WACS.Services.Legacy
{
    /// <summary>
    /// Forwards compatible classes to support importing renewals for the external library
    /// Should match up with AzureOptions in the other project
    /// </summary>
    internal class CompatibleHttpOptions : HttpValidationOptions
    {
        public CompatibleHttpOptions(string plugin) => Plugin = plugin;

        /// <summary>
        /// Credentials to use for WebDav connection
        /// </summary>
        public NetworkCredentialOptions? Credential { get; set; }
    }
}
