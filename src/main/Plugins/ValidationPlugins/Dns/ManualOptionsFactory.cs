using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class ManualOptionsFactory : ValidationPluginOptionsFactory<Manual, ManualOptions>
    {
        public ManualOptionsFactory(ILogService log) : base(log, Constants.Dns01ChallengeType) { }

        public override ManualOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return new ManualOptions();
        }

        public override ManualOptions Default(Target target, IOptionsService optionsService)
        {
            return new ManualOptions();
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
