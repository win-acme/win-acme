using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.ValidationPlugins;
namespace PKISharp.WACS.Services.Legacy
{
    /// <summary>
    /// Forwards compatible classes to support importing renewals for the external library
    /// Should match up with AzureOptions in the other project
    /// </summary>
    internal class CompatibleFtpOptions : HttpValidationOptions
    {
        public CompatibleFtpOptions() => Plugin = "bc27d719-dcf2-41ff-bf08-54db7ea49c48";

        /// <summary>
        /// Credentials to use for WebDav connection
        /// </summary>
        public NetworkCredentialOptions? Credential { get; set; }
    }
}
