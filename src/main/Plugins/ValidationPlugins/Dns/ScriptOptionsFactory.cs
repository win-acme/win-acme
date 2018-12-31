using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ScriptOptionsFactory : ValidationPluginOptionsFactory<Script, ScriptOptions>
    {
        public ScriptOptionsFactory(ILogService log) : base(log, Dns01ChallengeValidationDetails.Dns01ChallengeType) { }

        public override ScriptOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var options = new DnsScriptOptions(optionsService, inputService, _log);
            return new ScriptOptions()
            {
                ScriptConfiguration = options
            };
        }

        public override ScriptOptions Default(Target target, IOptionsService optionsService)
        {
            var options = new DnsScriptOptions(optionsService, _log);
            return new ScriptOptions()
            {
                ScriptConfiguration = options
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }

}
