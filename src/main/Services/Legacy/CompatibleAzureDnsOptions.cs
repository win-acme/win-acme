using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Services.Legacy
{
    /// <summary>
    /// Forwards compatible classes to support importing renewals for the external library
    /// Should match up with AzureOptions in the other project
    /// </summary>
    internal class CompatibleAzureOptions : ValidationPluginOptions
    {
        public override string Plugin => "PKISharp.WACS.Plugins.ValidationPlugins.Dns.Azure";
        public string ClientId { get; set; }
        public string ResourceGroupName { get; set; }
        public string SecretSafe { get; set; }
        [JsonIgnore]
        public string Secret
        {
            get => SecretSafe.Unprotect();
            set => SecretSafe = value.Protect();
        }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
    }
}