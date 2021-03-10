using Fclp;
using PKISharp.WACS.Plugins.Azure.Common;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class KeyVaultArgumentsProvider : AzureArgumentsProviderCommon<KeyVaultArguments>
    {
        public override string Name => "Azure KeyVault";
        public override string Group => "Store";
        public override string Condition => "--store keyvault";
        public override void Configure(FluentCommandLineParser<KeyVaultArguments> parser)
        {
            base.Configure(parser);
            _ = parser.Setup(o => o.VaultName)
                .As("vaultname")
                .WithDescription("The name of the vault");
            _ = parser.Setup(o => o.CertificateName)
                .As("certificatename")
                .WithDescription("The name of the certificate");
        }
    }
}
