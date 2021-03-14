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
        private readonly IArgumentsService _arguments;

        public KeyVaultOptionsFactory(IArgumentsService arguments) : base() => _arguments = arguments;

        public override async Task<KeyVaultOptions> Aquire(IInputService input, RunLevel runLevel)
        {
            var options = new KeyVaultOptions();
            var common = new AzureOptionsFactoryCommon<KeyVaultArguments>(_arguments, input);
            await common.Aquire(options);
            var az = _arguments.GetArguments<KeyVaultArguments>();
            options.VaultName = await _arguments.TryGetArgument(az.VaultName, input, "Vault name");
            options.CertificateName = await _arguments.TryGetArgument(az.CertificateName, input, "Certificate name");
            return options;
        }

        public override async Task<KeyVaultOptions> Default()
        {
            var options = new KeyVaultOptions();
            var common = new AzureOptionsFactoryCommon<KeyVaultArguments>(_arguments, null);
            await common.Default(options);
            var az = _arguments.GetArguments<KeyVaultArguments>();
            options.VaultName = _arguments.TryGetRequiredArgument(nameof(az.VaultName), az.VaultName);
            options.CertificateName = _arguments.TryGetRequiredArgument(nameof(az.CertificateName), az.CertificateName);
            return options;
        }
    }
}
