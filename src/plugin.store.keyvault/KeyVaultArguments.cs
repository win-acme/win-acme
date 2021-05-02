using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Azure.Common;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class KeyVaultArguments : AzureArgumentsCommon
    {
        public override string Name => "Azure KeyVault";
        public override string Group => "Store";
        public override string Condition => "--store keyvault";

        [CommandLine(Description = "The name of the vault")]
        public string VaultName { get; set; }

        [CommandLine(Description = "The name of the certificate")]
        public string CertificateName { get; set; }
    }
}