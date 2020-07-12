using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
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

            var options = new AzureOptions();

            var environments = new List<Choice<Func<Task>>>(
                AzureEnvironments.ResourceManagerUrls
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp =>
                        Choice.Create<Func<Task>>(() =>
                        {
                            options.AzureEnvironment = kvp.Key;
                            return Task.CompletedTask;
                        },
                    description: kvp.Key,
                    @default: kvp.Key == AzureEnvironments.AzureCloud)))
            {
                Choice.Create<Func<Task>>(async () => await InputUrl(input, options), "Use a custom resource manager url")
            };

            var chosen = await input.ChooseFromMenu("Which Azure environment are you using?", environments);
            await chosen.Invoke();

            options.UseMsi = az.AzureUseMsi || await input.PromptYesNo("Do you want to use a managed service identity?", true);

            if (!options.UseMsi)
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
                AzureEnvironment = az.AzureEnvironment,
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

        private async Task InputUrl(IInputService input, AzureOptions options)
        {
            string raw;
            do
            {
                raw = await input.RequestString("Url");
            }
            while (!ParseUrl(raw, options));
        }

        private bool ParseUrl(string url, AzureOptions options)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }
            try
            {
                var uri = new Uri(url);
                options.AzureEnvironment = uri.ToString();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
