using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Legacy
{
    /// <summary>
    /// Forwards compatible classes to support importing renewals for the external library
    /// Should match up with AzureOptions in the other project
    /// </summary>
    internal class CompatibleAzureOptions : ValidationPluginOptions
    {
        public CompatibleAzureOptions() => Plugin = "aa57b028-45fb-4aca-9cac-a63d94c76b4a";
        public string? ClientId { get; set; }
        public string? ResourceGroupName { get; set; }

        [JsonPropertyName("SecretSafe")]
        public ProtectedString? Secret { get; set; }

        public string? SubscriptionId { get; set; }
        public string? TenantId { get; set; }
    }
}