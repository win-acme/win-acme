using Fclp;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class AzureArgumentsProvide : BaseArgumentsProvider<AzureArguments>
    {
        public override string Name => "Azure";

        public override void Configure(FluentCommandLineParser<AzureArguments> parser)
        {
            parser.Setup(o => o.AzureTenantId)
                .As("azuretenantid")
                .WithDescription("[--validationmode dns-01 --validation azure] Tenant ID to login into Microsoft Azure.");
            parser.Setup(o => o.AzureClientId)
                .As("azureclientid")
                .WithDescription("[--validationmode dns-01 --validation azure] Client ID to login into Microsoft Azure.");
            parser.Setup(o => o.AzureSecret)
                .As("azuresecret")
                .WithDescription("[--validationmode dns-01 --validation azure] Secret to login into Microsoft Azure.");
            parser.Setup(o => o.AzureSubscriptionId)
                .As("azuresubscriptionid")
                .WithDescription("[--validationmode dns-01 --validation azure] Subscription ID to login into Microsoft Azure DNS.");
            parser.Setup(o => o.AzureResourceGroupName)
                .As("azureresourcegroupname")
                .WithDescription("[--validationmode dns-01 --validation azure] The name of the resource group within Microsoft Azure DNS.");
        }
    }
}
