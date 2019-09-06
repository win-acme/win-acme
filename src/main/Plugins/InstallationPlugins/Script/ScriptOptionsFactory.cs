using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class ScriptOptionsFactory : InstallationPluginFactory<Script, ScriptOptions>
    {
        public override int Order => 100;
        private ILogService _log;
        private IArgumentsService _arguments;

        public ScriptOptionsFactory(ILogService log, IArgumentsService arguments)
        {
            _log = log;
            _arguments = arguments;
        }

        public override ScriptOptions Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            var ret = new ScriptOptions();
            var args = _arguments.GetArguments<ScriptArguments>();
            inputService.Show("Full instructions", "https://github.com/PKISharp/win-acme/wiki/Install-Script");
            do
            {
                ret.Script = _arguments.TryGetArgument(args.Script, inputService, "Enter the path to the script that you want to run after renewal");
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

            ret.ScriptParameters = _arguments.TryGetArgument(args.ScriptParameters, inputService, "Enter the parameter format string for the script, e.g. \"--hostname {CertCommonName}\"");
            return ret;
        }

        public override ScriptOptions Default(Target target)
        {
            var args = _arguments.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions
            {
                Script = _arguments.TryGetRequiredArgument(nameof(args.Script), args.Script)
            };
            if (!ret.Script.ValidFile(_log))
            {
                throw new ArgumentException(nameof(args.Script));
            }
            ret.ScriptParameters = args.ScriptParameters;
            return ret;
        }
    }
}
