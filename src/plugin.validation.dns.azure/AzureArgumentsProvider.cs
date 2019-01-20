using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class AzureArgumentsProvider : BaseArgumentsProvider<AzureArguments>
    {
        public override string Name => "Azure";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation azure";

        public override bool Active(AzureArguments current)
        {
            return !string.IsNullOrEmpty(current.AzureTenantId) ||
                !string.IsNullOrEmpty(current.AzureClientId) ||
                !string.IsNullOrEmpty(current.AzureSecret) ||
                !string.IsNullOrEmpty(current.AzureSubscriptionId) ||
                !string.IsNullOrEmpty(current.AzureResourceGroupName);
        }

        public override void Configure(FluentCommandLineParser<AzureArguments> parser)
        {
            parser.Setup(o => o.AzureTenantId)
                .As("azuretenantid")
                .WithDescription("Tenant ID to login into Microsoft Azure.");
            parser.Setup(o => o.AzureClientId)
                .As("azureclientid")
                .WithDescription("Client ID to login into Microsoft Azure.");
            parser.Setup(o => o.AzureSecret)
                .As("azuresecret")
                .WithDescription("Secret to login into Microsoft Azure.");
            parser.Setup(o => o.AzureSubscriptionId)
                .As("azuresubscriptionid")
                .WithDescription("Subscription ID to login into Microsoft Azure DNS.");
            parser.Setup(o => o.AzureResourceGroupName)
                .As("azureresourcegroupname")
                .WithDescription("The name of the resource group within Microsoft Azure DNS.");
        }
    }
}
