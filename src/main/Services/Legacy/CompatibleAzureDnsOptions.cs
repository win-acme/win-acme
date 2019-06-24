using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Services.Legacy
{
    /// <summary>
    /// Forwards compatible classes to support importing renewals for the external library
    /// Should match up with AzureOptions in the other project
    /// </summary>
    [Plugin("aa57b028-45fb-4aca-9cac-a63d94c76b4a")]
    internal class CompatibleAzureOptions : ValidationPluginOptions, IIgnore
    {
        public string ClientId { get; set; }
        public string ResourceGroupName { get; set; }

        [JsonProperty(propertyName: "SecretSafe")]
        [JsonConverter(typeof(ProtectedStringConverter))]
        public string Secret { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }
    }
}