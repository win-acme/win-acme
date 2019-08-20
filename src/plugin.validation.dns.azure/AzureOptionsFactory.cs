using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    internal class AzureOptionsFactory : ValidationPluginOptionsFactory<Azure, AzureOptions>
    {
        public AzureOptionsFactory(ILogService log) : base(log, Dns01ChallengeValidationDetails.Dns01ChallengeType) { }

        public override AzureOptions Aquire(Target target, IArgumentsService options, IInputService input, RunLevel runLevel)
        {
            var az = options.GetArguments<AzureArguments>();
            return new AzureOptions()
            {
                TenantId = options.TryGetArgument(az.AzureTenantId, input, "Directory/tenant id"),
                ClientId = options.TryGetArgument(az.AzureClientId, input, "Application client id"),
                Secret = new ProtectedString(options.TryGetArgument(az.AzureSecret, input, "Application client secret", true)),
                SubscriptionId = options.TryGetArgument(az.AzureSubscriptionId, input, "DNS subscription id"),
                ResourceGroupName = options.TryGetArgument(az.AzureResourceGroupName, input, "DNS resoure group name")
            };
        }

        public override AzureOptions Default(Target target, IArgumentsService options)
        {
            var az = options.GetArguments<AzureArguments>();
            return new AzureOptions()
            {
                TenantId = options.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureTenantId),
                ClientId = options.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureClientId),
                Secret = new ProtectedString(options.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureSecret)),
                SubscriptionId = options.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureSubscriptionId),
                ResourceGroupName = options.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureResourceGroupName)
            };
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
