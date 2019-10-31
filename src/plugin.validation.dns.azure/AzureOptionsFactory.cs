using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
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
            var az = _arguments.GetArguments<AzureArguments>();
            return new AzureOptions()
            {
                TenantId = await _arguments.TryGetArgument(az.AzureTenantId, input, "Directory/tenant id"),
                ClientId = await _arguments.TryGetArgument(az.AzureClientId, input, "Application client id"),
                Secret = new ProtectedString(await _arguments.TryGetArgument(az.AzureSecret, input, "Application client secret", true)),
                SubscriptionId = await _arguments.TryGetArgument(az.AzureSubscriptionId, input, "DNS subscription id"),
                ResourceGroupName = await _arguments.TryGetArgument(az.AzureResourceGroupName, input, "DNS resoure group name")
            };
        }

        public override Task<AzureOptions> Default(Target target)
        {
            var az = _arguments.GetArguments<AzureArguments>();
            return Task.FromResult(new AzureOptions()
            {
                TenantId = _arguments.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureTenantId),
                ClientId = _arguments.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureClientId),
                Secret = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureSecret)),
                SubscriptionId = _arguments.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureSubscriptionId),
                ResourceGroupName = _arguments.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureResourceGroupName)
            });
        }

        public override bool CanValidate(Target target) => true;
    }
}
