using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class AzureOptionsFactory : ValidationPluginOptionsFactory<Azure, AzureOptions>
    {
        public AzureOptionsFactory(ILogService log) : base(log, Dns01ChallengeValidationDetails.Dns01ChallengeType) { }

        public override AzureOptions Aquire(Target target, IOptionsService options, IInputService input, RunLevel runLevel)
        {
            var az = options.GetArguments<AzureArguments>();
            return new AzureOptions()
            {
                TenantId = options.TryGetOption(az.AzureTenantId, input, "Tenant Id"),
                ClientId = options.TryGetOption(az.AzureClientId, input, "Client Id"),
                Secret = options.TryGetOption(az.AzureSecret, input, "Secret", true),
                SubscriptionId = options.TryGetOption(az.AzureSubscriptionId, input, "DNS Subscription ID"),
                ResourceGroupName = options.TryGetOption(az.AzureResourceGroupName, input, "DNS Resoure Group Name")
            };
        }

        public override AzureOptions Default(Target target, IOptionsService options)
        {
            var az = options.GetArguments<AzureArguments>();
            return new AzureOptions()
            {
                TenantId = options.TryGetRequiredOption(nameof(az.AzureTenantId), az.AzureTenantId),
                ClientId = options.TryGetRequiredOption(nameof(az.AzureTenantId), az.AzureClientId),
                Secret = options.TryGetRequiredOption(nameof(az.AzureTenantId), az.AzureSecret),
                SubscriptionId = options.TryGetRequiredOption(nameof(az.AzureTenantId), az.AzureSubscriptionId),
                ResourceGroupName = options.TryGetRequiredOption(nameof(az.AzureTenantId), az.AzureResourceGroupName)
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
