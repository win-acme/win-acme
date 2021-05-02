using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.Azure.Common
{
    public abstract class AzureArgumentsCommon : BaseArguments
    {
        [CommandLine(Description = "This can be used to specify a specific Azure endpoint. " +
                "Valid inputs are AzureCloud (default), AzureChinaCloud, AzureGermanCloud, " +
                "AzureUSGovernment or a specific URI for an Azure Stack implementation.")]
        public string AzureEnvironment { get; set; }

        [CommandLine(Description = "Use Managed Service Identity for authentication.")]
        public bool AzureUseMsi { get; set; }

        [CommandLine(Description = "Directory/tenant identifier. Found in Azure AD > Properties.")]
        public string AzureTenantId { get; set; }

        [CommandLine(Description = "Application/client identifier. Found/created in Azure AD > App registrations.")]
        public string AzureClientId { get; set; }

        [CommandLine(Description = "Client secret. Found/created under Azure AD > App registrations.")]
        public string AzureSecret { get; set; }
    }
}