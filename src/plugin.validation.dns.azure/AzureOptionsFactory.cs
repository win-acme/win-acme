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
            return new AzureOptions()
            {
                TenantId = options.TryGetOption(options.Options.AzureTenantId, input, "Tenant Id"),
                ClientId = options.TryGetOption(options.Options.AzureClientId, input, "Client Id"),
                Secret = options.TryGetOption(options.Options.AzureSecret, input, "Secret", true),
                SubscriptionId = options.TryGetOption(options.Options.AzureSubscriptionId, input, "DNS Subscription ID"),
                ResourceGroupName = options.TryGetOption(options.Options.AzureResourceGroupName, input, "DNS Resoure Group Name")
            };
        }

        public override AzureOptions Default(Target target, IOptionsService options)
        {
            return new AzureOptions()
            {
                TenantId = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureTenantId),
                ClientId = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureClientId),
                Secret = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureSecret),
                SubscriptionId = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureSubscriptionId),
                ResourceGroupName = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureResourceGroupName)
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
