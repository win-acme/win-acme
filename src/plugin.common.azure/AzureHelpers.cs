using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Azure.Common
{
    public class AzureHelpers
    {
        private readonly IAzureOptionsCommon _options;
        public Uri ResourceManagersEndpoint { get; private set; }
        public AzureHelpers(IAzureOptionsCommon options, ILogService log)
        {
            _options = options;
            ResourceManagersEndpoint = new Uri(AzureEnvironments.ResourceManagerUrls[AzureEnvironments.AzureCloud]);
            if (!string.IsNullOrEmpty(options.AzureEnvironment))
            {
                if (!AzureEnvironments.ResourceManagerUrls.TryGetValue(options.AzureEnvironment, out var endpoint))
                {
                    // Custom endpoint 
                    endpoint = options.AzureEnvironment;
                }
                try
                {
                    ResourceManagersEndpoint = new Uri(endpoint);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Could not parse Azure endpoint url. Falling back to default.");
                }
            }
        }

        /// <summary>
        /// Retrieve active directory settings based on the current Azure environment
        /// </summary>
        /// <returns></returns>
        private ActiveDirectoryServiceSettings GetActiveDirectorySettingsForAzureEnvironment()
        {
            return _options.AzureEnvironment switch
            {
                AzureEnvironments.AzureChinaCloud => ActiveDirectoryServiceSettings.AzureChina,
                AzureEnvironments.AzureUSGovernment => ActiveDirectoryServiceSettings.AzureUSGovernment,
                AzureEnvironments.AzureGermanCloud => ActiveDirectoryServiceSettings.AzureGermany,
                _ => ActiveDirectoryServiceSettings.Azure,
            };
        }

        public async Task<ServiceClientCredentials> GetCredentials()
        {
            // Build the service credentials and DNS management client
            ServiceClientCredentials credentials;

            // Decide between Managed Service Identity (MSI) 
            // and service principal with client credentials
            if (_options.UseMsi)
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(ResourceManagersEndpoint.ToString());
                credentials = new TokenCredentials(accessToken);
            }
            else
            {
                credentials = await ApplicationTokenProvider.LoginSilentAsync(
                    _options.TenantId,
                    _options.ClientId,
                    _options.Secret?.Value,
                    GetActiveDirectorySettingsForAzureEnvironment());
            }
            return credentials;
        }
    }
}
