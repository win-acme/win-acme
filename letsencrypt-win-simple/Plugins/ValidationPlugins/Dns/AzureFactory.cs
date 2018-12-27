using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class AzureFactory : BaseValidationPluginFactory<Azure, AzureOptions>
    {
        public AzureFactory(ILogService log) : base(log, Dns01ChallengeValidationDetails.Dns01ChallengeType) { }

        public override AzureOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var config = new AzureDnsOptions(optionsService, inputService);
            return new AzureOptions() { AzureConfiguration = config };
        }

        public override AzureOptions Default(Target target, IOptionsService optionsService)
        {
            var config = new AzureDnsOptions(optionsService);
            return new AzureOptions() { AzureConfiguration = config };
        }
    }
}
