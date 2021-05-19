using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class AzureOptionsFactory : ValidationPluginOptionsFactory<Azure, AzureOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public AzureOptionsFactory(ArgumentsInputService arguments) : 
            base(Dns01ChallengeValidationDetails.Dns01ChallengeType) 
            => _arguments = arguments;

        private ArgumentResult<string> ResourceGroupName => _arguments.
            GetString<AzureArguments>(a => a.AzureResourceGroupName).
            Required();

        private ArgumentResult<string> SubscriptionId => _arguments.
            GetString<AzureArguments>(a => a.AzureSubscriptionId).
            Required();

        private ArgumentResult<string> HostedZone => _arguments.
             GetString<AzureArguments>(a => a.AzureHostedZone);

        public override async Task<AzureOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var options = new AzureOptions();
            var common = new AzureOptionsFactoryCommon<AzureArguments>(_arguments, input);
            await common.Aquire(options);
            options.ResourceGroupName = await ResourceGroupName.Interactive(input).GetValue();
            options.SubscriptionId = await SubscriptionId.Interactive(input).GetValue();
            options.HostedZone = await HostedZone.Interactive(input).GetValue();
            return options;
        }

        public override async Task<AzureOptions> Default(Target target)
        {
            var options = new AzureOptions();
            var common = new AzureOptionsFactoryCommon<AzureArguments>(_arguments, null);
            await common.Default(options);
            options.ResourceGroupName = await ResourceGroupName.GetValue();
            options.SubscriptionId = await SubscriptionId.GetValue();
            options.HostedZone = await HostedZone.GetValue();
            return options;
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
