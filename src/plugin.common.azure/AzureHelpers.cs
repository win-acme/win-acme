using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.ResourceManager;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Azure.Common
{
    public class AzureHelpers
    {
        private readonly IAzureOptionsCommon _options;
        private readonly SecretServiceManager _ssm;
        private readonly IProxyService _proxyService;

        public AzureHelpers(
            IAzureOptionsCommon options,
            IProxyService proxy,
            SecretServiceManager ssm)
        {
            _options = options;
            _ssm = ssm;
            _proxyService = proxy;
        }

        /// <summary>
        /// Retrieve active directory settings based on the current Azure environment
        /// </summary>
        /// <returns></returns>
        private ArmEnvironment ArmEnvironment
        {
            get {
                if (string.IsNullOrWhiteSpace(_options.AzureEnvironment))
                {
                    return ArmEnvironment.AzurePublicCloud;
                }
                return _options.AzureEnvironment switch
                {
                    AzureEnvironments.AzureChinaCloud => ArmEnvironment.AzureChina,
                    AzureEnvironments.AzureUSGovernment => ArmEnvironment.AzureGovernment,
                    AzureEnvironments.AzureGermanCloud => ArmEnvironment.AzureGermany,
                    AzureEnvironments.AzureCloud => ArmEnvironment.AzurePublicCloud,
                    null => ArmEnvironment.AzurePublicCloud,
                    "" => ArmEnvironment.AzurePublicCloud,
                    _ => new ArmEnvironment(new Uri(_options.AzureEnvironment), _options.AzureEnvironment)
                };
            }
        }

        public TokenCredential TokenCredential
        {
            get
            {
                return _options.UseMsi
                      ? new ManagedIdentityCredential()
                      : new ClientSecretCredential(
                          _options.TenantId,
                          _options.ClientId,
                          _ssm.EvaluateSecret(_options.Secret?.Value));
            }
        }

        public ArmClientOptions ArmOptions
        {
            get 
            {
                return new ArmClientOptions() { 
                    Environment = ArmEnvironment,
                    Transport = new HttpClientTransport(_proxyService.GetHttpClient())
                };
            }
        }
    }
}
