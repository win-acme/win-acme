using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Azure.Common
{
    public static class AzureEnvironments
    {
        public const string AzureCloud = "AzureCloud";

        public const string AzureChinaCloud = "AzureChinaCloud";

        public const string AzureUSGovernment = "AzureUSGovernment";

        public const string AzureGermanCloud = "AzureGermanCloud";

        public static IDictionary<string, string> ResourceManagerUrls
            = new ConcurrentDictionary<string, string>()
            {
                [AzureCloud] = "https://management.azure.com",
                [AzureChinaCloud] = "https://management.chinacloudapi.cn",
                [AzureUSGovernment] = "https://management.usgovcloudapi.net",
                [AzureGermanCloud] = "https://management.microsoftazure.de",
            };
    }
}
