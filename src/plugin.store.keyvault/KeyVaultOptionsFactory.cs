using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure Key Vault
    /// </summary>
    internal class KeyVaultOptionsFactory : StorePluginOptionsFactory<KeyVault, KeyVaultOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public KeyVaultOptionsFactory(ArgumentsInputService arguments) : base() => _arguments = arguments;

        private ArgumentResult<string> VaultName => _arguments.
            GetString<KeyVaultArguments>(a => a.VaultName).
            Required();

        private ArgumentResult<string> CertificateName => _arguments.
            GetString<KeyVaultArguments>(a => a.CertificateName).
            Required();

        public override async Task<KeyVaultOptions> Aquire(IInputService input, RunLevel runLevel)
        {
            var options = new KeyVaultOptions();
            var common = new AzureOptionsFactoryCommon<KeyVaultArguments>(_arguments, input);
            await common.Aquire(options);
            options.VaultName = await VaultName.Interactive(input).GetValue();
            options.CertificateName = await CertificateName.Interactive(input).GetValue();
            return options;
        }

        public override async Task<KeyVaultOptions> Default()
        {
            var options = new KeyVaultOptions();
            var common = new AzureOptionsFactoryCommon<KeyVaultArguments>(_arguments, null);
            await common.Default(options);
            options.VaultName = await VaultName.GetValue();
            options.CertificateName = await CertificateName.GetValue();
            return options;
        }
    }
}
