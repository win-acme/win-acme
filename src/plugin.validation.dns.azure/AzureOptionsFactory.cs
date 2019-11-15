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

            var useMsi = az.AzureUseMsi || await input.PromptYesNo("Do you want to use a managed service identity?", true);
            var options = new AzureOptions
            {
                UseMsi = useMsi,
            };
            
            if (!useMsi)
            {
                // These options are only necessary for client id/secret authentication.
                options.TenantId = await _arguments.TryGetArgument(az.AzureTenantId, input, "Directory/tenant id");
                options.ClientId = await _arguments.TryGetArgument(az.AzureClientId, input, "Application client id");
                options.Secret = new ProtectedString(await _arguments.TryGetArgument(az.AzureSecret, input,"Application client secret", true));
            }

            options.SubscriptionId = await _arguments.TryGetArgument(az.AzureSubscriptionId, input, "DNS subscription id");
            options.ResourceGroupName = await _arguments.TryGetArgument(az.AzureResourceGroupName, input, "DNS resource group name");
           
            return options;
        }

        public override Task<AzureOptions> Default(Target target)
        {
            var az = _arguments.GetArguments<AzureArguments>();
            var options = new AzureOptions
            {
                UseMsi = az.AzureUseMsi,
                SubscriptionId = _arguments.TryGetRequiredArgument(nameof(az.AzureSubscriptionId), az.AzureSubscriptionId),
                ResourceGroupName = _arguments.TryGetRequiredArgument(nameof(az.AzureResourceGroupName), az.AzureResourceGroupName)
            };

            if (!options.UseMsi)
            {
                // These options are only necessary for client id/secret authentication.
                options.TenantId = _arguments.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureTenantId);
                options.ClientId = _arguments.TryGetRequiredArgument(nameof(az.AzureClientId), az.AzureClientId);
                options.Secret = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(az.AzureSecret), az.AzureSecret));
            }

            return Task.FromResult(options);
        }

        public override bool CanValidate(Target target) => true;
    }
}
