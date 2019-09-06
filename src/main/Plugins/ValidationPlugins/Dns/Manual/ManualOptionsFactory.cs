using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class ManualOptionsFactory : ValidationPluginOptionsFactory<Manual, ManualOptions>
    {
        public ManualOptionsFactory() : base(Constants.Dns01ChallengeType) { }

        public override ManualOptions Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            return new ManualOptions();
        }

        public override ManualOptions Default(Target target)
        {
            return new ManualOptions();
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
