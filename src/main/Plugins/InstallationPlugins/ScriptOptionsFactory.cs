using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class ScriptOptionsFactory : InstallationPluginFactory<Script, ScriptOptions>
    {
        public ScriptOptionsFactory(ILogService log) : base(log) { }

        public override ScriptOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var ret = new ScriptOptions();
            inputService.Show("Full instructions", "https://github.com/PKISharp/win-acme/wiki/Install-Script");
            do
            {
                ret.Script = optionsService.TryGetOption(optionsService.MainArguments.Script, inputService, "Enter the path to the script that you want to run after renewal");
            }
            while (!ret.Script.ValidFile(_log));
            inputService.Show("{0}", "Common name");
            inputService.Show("{1}", ".pfx password");
            inputService.Show("{2}", ".pfx path");
            inputService.Show("{3}", "Store name");
            inputService.Show("{4}", "Friendly name");
            inputService.Show("{5}", "Certificate thumbprint");
            ret.ScriptParameters = optionsService.TryGetOption(optionsService.MainArguments.ScriptParameters, inputService, "Enter the parameter format string for the script, e.g. \"--hostname {0}\"");
            return ret;
        }

        public override ScriptOptions Default(Target target, IOptionsService optionsService)
        {
            var ret = new ScriptOptions
            {
                Script = optionsService.TryGetRequiredOption(nameof(optionsService.MainArguments.Script), optionsService.MainArguments.Script)
            };
            if (!ret.Script.ValidFile(_log))
            {
                throw new ArgumentException(nameof(optionsService.MainArguments.Script));
            }
            ret.ScriptParameters = optionsService.MainArguments.ScriptParameters;
            return ret;
        }
    }
}
