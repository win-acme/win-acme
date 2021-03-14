using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Azure.Common
{
    /// <summary>
    /// Azure common options
    /// </summary>
    public class AzureOptionsFactoryCommon<T> where T: AzureArgumentsCommon, new()
    {
        private readonly IArgumentsService _arguments;
        private readonly IInputService _input;

        public AzureOptionsFactoryCommon(IArgumentsService arguments, IInputService input)
        {
            _arguments = arguments;
            _input = input;
        }

        public async Task Aquire(IAzureOptionsCommon options)
        {
            var az = _arguments.GetArguments<T>();
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
                Choice.Create<Func<Task>>(async () => await InputUrl(_input, options), "Use a custom resource manager url")
            };

            var chosen = await _input.ChooseFromMenu("Which Azure environment are you using?", environments);
            await chosen.Invoke();
            options.UseMsi = az.AzureUseMsi || await _input.PromptYesNo("Do you want to use a managed service identity?", true);
            if (!options.UseMsi)
            {
                // These options are only necessary for client id/secret authentication.
                options.TenantId = await _arguments.TryGetArgument(az.AzureTenantId, _input, "Directory/tenant id");
                options.ClientId = await _arguments.TryGetArgument(az.AzureClientId, _input, "Application client id");
                options.Secret = new ProtectedString(await _arguments.TryGetArgument(az.AzureSecret, _input, "Application client secret", true));
            }
        }

        public Task Default(IAzureOptionsCommon options)
        {
            var az = _arguments.GetArguments<T>();
            options.UseMsi = az.AzureUseMsi;
            options.AzureEnvironment = az.AzureEnvironment;
            if (!options.UseMsi)
            {
                // These options are only necessary for client id/secret authentication.
                options.TenantId = _arguments.TryGetRequiredArgument(nameof(az.AzureTenantId), az.AzureTenantId);
                options.ClientId = _arguments.TryGetRequiredArgument(nameof(az.AzureClientId), az.AzureClientId);
                options.Secret = new ProtectedString(_arguments.TryGetRequiredArgument(nameof(az.AzureSecret), az.AzureSecret));
            }
            return Task.CompletedTask;
        }

        private async Task InputUrl(IInputService input, IAzureOptionsCommon options)
        {
            string raw;
            do
            {
                raw = await input.RequestString("Url");
            }
            while (!ParseUrl(raw, options));
        }

        private bool ParseUrl(string url, IAzureOptionsCommon options)
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
