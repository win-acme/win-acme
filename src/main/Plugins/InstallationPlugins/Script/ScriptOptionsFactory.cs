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

        public override ScriptOptions Aquire(Target target, IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            var ret = new ScriptOptions();
            var args = arguments.GetArguments<ScriptArguments>();
            inputService.Show("Full instructions", "https://github.com/PKISharp/win-acme/wiki/Install-Script");
            do
            {
                ret.Script = arguments.TryGetArgument(args.Script, inputService, "Enter the path to the script that you want to run after renewal");
            }
            while (!ret.Script.ValidFile(_log));
            inputService.Show("{0}", "Common name");
            inputService.Show("{1}", ".pfx password");
            inputService.Show("{2}", ".pfx path");
            inputService.Show("{3}", "Store name");
            inputService.Show("{4}", "Friendly name");
            inputService.Show("{5}", "Certificate thumbprint");
            ret.ScriptParameters = arguments.TryGetArgument(args.ScriptParameters, inputService, "Enter the parameter format string for the script, e.g. \"--hostname {0}\"");
            return ret;
        }

        public override ScriptOptions Default(Target target, IArgumentsService arguments)
        {
            var args = arguments.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions
            {
                Script = arguments.TryGetRequiredArgument(nameof(args.Script), args.Script)
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
