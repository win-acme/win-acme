using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("aa57b028-45fb-4aca-9cac-a63d94c76b4a")]
    internal class AzureOptions : ValidationPluginOptions<Azure>, IAzureOptionsCommon
    {
        public override string Name => "Azure";
        public override string Description => "Create verification records in Azure DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string? AzureEnvironment { get; set; }
        public bool UseMsi { get; set; }
        public string? ClientId { get; set; }
        public string? ResourceGroupName { get; set; }

        [JsonProperty(propertyName: "SecretSafe")]
        public ProtectedString? Secret { get; set; }

        public string? SubscriptionId { get; set; }
        public string? TenantId { get; set; }
        public string? HostedZone { get; set; }
    }
}
