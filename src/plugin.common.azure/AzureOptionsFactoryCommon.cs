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
        private readonly ArgumentsInputService _arguments;
        private readonly IInputService _input;

        public AzureOptionsFactoryCommon(ArgumentsInputService arguments, IInputService input)
        {
            _arguments = arguments;
            _input = input;
        }
        private ArgumentResult<string> Environment => _arguments.
            GetString<T>(a => a.AzureEnvironment);

        private ArgumentResult<bool?> UseMsi => _arguments.
            GetBool<T>(a => a.AzureUseMsi);

        private ArgumentResult<string> TenantId => _arguments.
            GetString<T>(a => a.AzureTenantId).
            Required();

        private ArgumentResult<string> ClientId => _arguments.
            GetString<T>(a => a.AzureClientId).
            Required();

        private ArgumentResult<ProtectedString> ClientSecret => _arguments.
            GetProtectedString<T>(a => a.AzureSecret).
            Required();

        public async Task Aquire(IAzureOptionsCommon options)
        {
            var defaultEnvironment = (await Environment.GetValue()) ?? AzureEnvironments.AzureCloud;
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
                    @default: kvp.Key == defaultEnvironment)))
            {
                Choice.Create<Func<Task>>(async () => await InputUrl(_input, options), "Use a custom resource manager url")
            };
            var chosen = await _input.ChooseFromMenu("Which Azure environment are you using?", environments);
            await chosen.Invoke();

            options.UseMsi = 
                await UseMsi.GetValue() == true || 
                await _input.PromptYesNo("Do you want to use a managed service identity?", false);

            if (!options.UseMsi)
            {
                // These options are only necessary for client id/secret authentication.
                options.TenantId = await TenantId.Interactive(_input, "Directory/tenant id").GetValue();
                options.ClientId = await ClientId.Interactive(_input, "Application client id").GetValue();
                options.Secret = await ClientSecret.Interactive(_input, "Application client secret").GetValue();
            }
        }

        public async Task Default(IAzureOptionsCommon options)
        {
            options.UseMsi = await UseMsi.GetValue() == true;
            options.AzureEnvironment = await Environment.GetValue();
            if (!options.UseMsi)
            {
                // These options are only necessary for client id/secret authentication.
                options.TenantId = await TenantId.GetValue();
                options.ClientId = await ClientId.GetValue();
                options.Secret = await ClientSecret.GetValue();
            }
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

        private static bool ParseUrl(string url, IAzureOptionsCommon options)
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
