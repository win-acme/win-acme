using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.Azure.Common
{
    public abstract class AzureArgumentsProviderCommon<T> : BaseArgumentsProvider<T> where T: AzureArgumentsCommon, new() 
    {
        public override void Configure(FluentCommandLineParser<T> parser)
        {
            _ = parser.Setup(o => o.AzureEnvironment)
                .As("azureenvironment")
                .WithDescription("This can be used to specify a specific Azure endpoint. " +
                "Valid inputs are AzureCloud (default), AzureChinaCloud, AzureGermanCloud, " +
                "AzureUSGovernment or a specific URI for an Azure Stack implementation.");
            _ = parser.Setup(o => o.AzureUseMsi)
                .As("azureusemsi")
                .WithDescription("Use Managed Service Identity for authentication.");
            _ = parser.Setup(o => o.AzureTenantId)
                .As("azuretenantid")
                .WithDescription("Tenant ID to login into Microsoft Azure.");
            _ = parser.Setup(o => o.AzureClientId)
                .As("azureclientid")
                .WithDescription("Client ID to login into Microsoft Azure.");
            _ = parser.Setup(o => o.AzureSecret)
                .As("azuresecret")
                .WithDescription("Secret to login into Microsoft Azure.");
        }
    }
}
