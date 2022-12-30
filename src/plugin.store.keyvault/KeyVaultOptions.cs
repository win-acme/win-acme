using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(KeyVaultOptions))]
    internal partial class KeyVaultJson : JsonSerializerContext
    {
        public KeyVaultJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class KeyVaultOptions : StorePluginOptions<KeyVault>, IAzureOptionsCommon
    {
        public override string Name => "KeyVault";
        public override string Description => "Store certificate in Azure Key Vault";

        public string? AzureEnvironment { get; set; }
        public bool UseMsi { get; set; }
        public string? ClientId { get; set; }
        public string? ResourceGroupName { get; set; }

        [JsonPropertyName("SecretSafe")]
        public ProtectedString? Secret { get; set; }

        public string? SubscriptionId { get; set; }
        public string? TenantId { get; set; }
        public string? VaultName { get; set; } = "";
        public string? CertificateName { get; set; } = ""; 
    }
}
