using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class ScriptOptionsFactory : InstallationPluginFactory<Script, ScriptOptions>
    {
        public override int Order => 100;
        private readonly ILogService _log;
        private readonly ArgumentsInputService _arguments;

        public ScriptOptionsFactory(ILogService log, ArgumentsInputService arguments)
        {
            _log = log;
            _arguments = arguments;
        }
        private ArgumentResult<string?> Script => _arguments.
            GetString<ScriptArguments>(x => x.Script).
            Validate(x => Task.FromResult(x.ValidFile(_log)), "invalid path").
            Validate(x => Task.FromResult(x!.EndsWith(".ps1") || x!.EndsWith(".exe") || x!.EndsWith(".bat")), "invalid extension").
            Required();

        private ArgumentResult<string?> Parameters => _arguments.
            GetString<ScriptArguments>(x => x.ScriptParameters);

        public override async Task<ScriptOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {

            inputService.CreateSpace();
            inputService.Show("Full instructions", "https://win-acme.com/reference/plugins/installation/script");
            inputService.CreateSpace();
            inputService.Show("{CertCommonName}", "Common name (primary domain name)");
            inputService.Show("{CachePassword}", ".pfx password");
            inputService.Show("{CacheFile}", ".pfx full path");
            inputService.Show("{CertFriendlyName}", "Certificate friendly name");
            inputService.Show("{CertThumbprint}", "Certificate thumbprint");
            inputService.Show("{StoreType}", $"Type of store ({CentralSslOptions.PluginName}/{CertificateStoreOptions.PluginName}/{PemFilesOptions.PluginName})");
            inputService.Show("{StorePath}", "Path to the store");
            inputService.Show("{RenewalId}", "Renewal identifier");
            inputService.Show("{OldCertCommonName}", "Common name (primary domain name) of the previously issued certificate");
            inputService.Show("{OldCertFriendlyName}", "Friendly name of the previously issued certificate");
            inputService.Show("{OldCertThumbprint}", "Thumbprint of the previously issued certificate");
            inputService.CreateSpace();

            return new ScriptOptions
            {
                Script = await Script.Interactive(inputService, "File").GetValue(),
                ScriptParameters = await Parameters.Interactive(inputService, "Parameters").GetValue()
            };
        }

        public override async Task<ScriptOptions> Default(Target target)
        {
            return new ScriptOptions
            {
                Script = await Script.GetValue(),
                ScriptParameters = await Parameters.GetValue()
            };
        }
    }
}
