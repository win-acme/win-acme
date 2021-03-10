using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class ScriptOptionsFactory : InstallationPluginFactory<Script, ScriptOptions>
    {
        public override int Order => 100;
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public ScriptOptionsFactory(ILogService log, IArgumentsService arguments)
        {
            _log = log;
            _arguments = arguments;
        }

        public override async Task<ScriptOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var ret = new ScriptOptions();
            var args = _arguments.GetArguments<ScriptArguments>();
            inputService.Show("Full instructions", "https://www.win-acme.com/reference/plugins/installation/script");
            do
            {
                ret.Script = await _arguments.TryGetArgument(args?.Script, inputService, "Enter the path to the script that you want to run after renewal");
            }
            while (!ret.Script.ValidFile(_log));

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
            ret.ScriptParameters = await _arguments.TryGetArgument(
                args?.ScriptParameters, 
                inputService, 
                "Enter the parameter format string for the script, e.g. \"--hostname {CertCommonName}\"");
            return ret;
        }

        public override Task<ScriptOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions
            {
                Script = _arguments.TryGetRequiredArgument(nameof(args.Script), args?.Script)
            };
            if (!ret.Script.ValidFile(_log))
            {
                throw new ArgumentException(nameof(args.Script));
            }
            ret.ScriptParameters = args?.ScriptParameters;
            return Task.FromResult(ret);
        }
    }
}
