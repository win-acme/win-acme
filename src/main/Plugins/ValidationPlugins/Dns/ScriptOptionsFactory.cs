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

        public override ScriptOptions Aquire(Target target, IOptionsService options, IInputService input, RunLevel runLevel)
        {
            var ret = new ScriptOptions();
            do
            {
                ret.CreateScript = options.TryGetOption(options.Options.DnsCreateScript, input, "Path to script that creates DNS records. Parameters passed are the hostname, record name and token");
            }
            while (!ret.CreateScript.ValidFile(_log));
            do
            {
                ret.DeleteScript = options.TryGetOption(options.Options.DnsDeleteScript, input, "Path to script that deletes DNS records. Parameters passed are the hostname and record name");
            }
            while (!ret.DeleteScript.ValidFile(_log));
            return ret;
        }

        public override ScriptOptions Default(Target target, IOptionsService options)
        {
            var ret = new ScriptOptions
            {
                CreateScript = options.TryGetRequiredOption(nameof(options.Options.DnsCreateScript), options.Options.DnsCreateScript),
                DeleteScript = options.TryGetRequiredOption(nameof(options.Options.DnsDeleteScript), options.Options.DnsDeleteScript)
            };           
            if (!ret.CreateScript.ValidFile(_log))
            {
                throw new ArgumentException(nameof(options.Options.DnsCreateScript));
            }
            if (!ret.DeleteScript.ValidFile(_log))
            {
                throw new ArgumentException(nameof(options.Options.DnsDeleteScript));
            }
            return ret;
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }

}
