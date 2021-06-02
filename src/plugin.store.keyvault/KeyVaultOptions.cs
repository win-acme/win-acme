using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("dbfa91e2-28c0-4b37-857c-df6575dbb388")]
    internal class KeyVaultOptions : StorePluginOptions<KeyVault>, IAzureOptionsCommon
    {
        public override string Name => "KeyVault";
        public override string Description => "Store certificate in Azure Key Vault";

        public string? AzureEnvironment { get; set; }
        public bool UseMsi { get; set; }
        public string? ClientId { get; set; }
        public string? ResourceGroupName { get; set; }

        [JsonProperty(propertyName: "SecretSafe")]
        public ProtectedString? Secret { get; set; }

        public string? SubscriptionId { get; set; }
        public string? TenantId { get; set; }
        public string? VaultName { get; set; } = "";
        public string? CertificateName { get; set; } = ""; 
    }
}
