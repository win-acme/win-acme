using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class AzureOptionsFactory : ValidationPluginOptionsFactory<Azure, AzureOptions>
    {
        private readonly IArgumentsService _arguments;

        public AzureOptionsFactory(IArgumentsService arguments) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) => _arguments = arguments;

        public override async Task<AzureOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var options = new AzureOptions();
            var az = _arguments.GetArguments<AzureArguments>();
            var common = new AzureOptionsFactoryCommon<AzureArguments>(_arguments, input);
            await common.Aquire(options);
            options.ResourceGroupName = await _arguments.TryGetArgument(az.AzureResourceGroupName, input, "DNS resource group name");
            options.SubscriptionId = await _arguments.TryGetArgument(az.AzureSubscriptionId, input, "Subscription id");
            options.HostedZone = await _arguments.TryGetArgument(az.AzureHostedZone, input, "Hosted Zone (blank to find best match)");
            return options;
        }

        public override async Task<AzureOptions> Default(Target target)
        {
            var options = new AzureOptions();
            var az = _arguments.GetArguments<AzureArguments>();
            var common = new AzureOptionsFactoryCommon<AzureArguments>(_arguments, null);
            await common.Default(options);
            options.ResourceGroupName = _arguments.TryGetRequiredArgument(nameof(az.AzureResourceGroupName), az.AzureResourceGroupName);
            options.SubscriptionId = _arguments.TryGetRequiredArgument(nameof(az.AzureSubscriptionId), az.AzureSubscriptionId);
            options.HostedZone = _arguments.TryGetRequiredArgument(nameof(az.AzureHostedZone), az.AzureHostedZone);
            return options;
        }

        public override bool CanValidate(Target target) => true;
    }
}
