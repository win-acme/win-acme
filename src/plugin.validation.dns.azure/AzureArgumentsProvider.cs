using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class AzureArgumentsProvider : BaseArgumentsProvider<AzureArguments>
    {
        public override string Name => "Azure";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation azure";
        public override void Configure(FluentCommandLineParser<AzureArguments> parser)
        {
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
            _ = parser.Setup(o => o.AzureSubscriptionId)
                .As("azuresubscriptionid")
                .WithDescription("Subscription ID to login into Microsoft Azure DNS.");
            _ = parser.Setup(o => o.AzureResourceGroupName)
                .As("azureresourcegroupname")
                .WithDescription("The name of the resource group within Microsoft Azure DNS.");
        }
    }
}
