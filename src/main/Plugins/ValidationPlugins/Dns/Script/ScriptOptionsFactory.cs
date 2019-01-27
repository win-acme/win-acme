using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ScriptOptionsFactory : ValidationPluginOptionsFactory<Script, ScriptOptions>
    {
        public ScriptOptionsFactory(ILogService log) : base(log, Constants.Dns01ChallengeType) { }

        public override ScriptOptions Aquire(Target target, IArgumentsService options, IInputService input, RunLevel runLevel)
        {
            var args = options.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions();
            do
            {
                ret.CreateScript = options.TryGetArgument(args.DnsCreateScript, input, "Path to script that creates DNS records. Parameters passed are the hostname, record name and token");
            }
            while (!ret.CreateScript.ValidFile(_log));
            do
            {
                ret.DeleteScript = options.TryGetArgument(args.DnsDeleteScript, input, "Path to script that deletes DNS records. Parameters passed are the hostname and record name");
            }
            while (!ret.DeleteScript.ValidFile(_log));
            return ret;
        }

        public override ScriptOptions Default(Target target, IArgumentsService arguments)
        {
            var args = arguments.GetArguments<ScriptArguments>();
            var ret = new ScriptOptions
            {
                CreateScript = arguments.TryGetRequiredArgument(nameof(args.DnsCreateScript), args.DnsCreateScript),
                DeleteScript = arguments.TryGetRequiredArgument(nameof(args.DnsDeleteScript), args.DnsDeleteScript)
            };           
            if (!ret.CreateScript.ValidFile(_log))
            {
                throw new ArgumentException(nameof(args.DnsCreateScript));
            }
            if (!ret.DeleteScript.ValidFile(_log))
            {
                throw new ArgumentException(nameof(args.DnsDeleteScript));
            }
            return ret;
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }

}
