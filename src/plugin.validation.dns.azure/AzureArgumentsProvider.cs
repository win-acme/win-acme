using Fclp;
using PKISharp.WACS.Plugins.Azure.Common;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class AzureArgumentsProvider : AzureArgumentsProviderCommon<AzureArguments>
    {
        public override string Name => "Azure";
        public override string Group => "Validation";
        public override string Condition => "--validationmode dns-01 --validation azure";
        public override void Configure(FluentCommandLineParser<AzureArguments> parser)
        {
            base.Configure(parser);
            _ = parser.Setup(o => o.AzureSubscriptionId)
                .As("azuresubscriptionid")
                .WithDescription("Subscription ID to login into Microsoft Azure DNS.");
            _ = parser.Setup(o => o.AzureResourceGroupName)
                .As("azureresourcegroupname")
                .WithDescription("[Obsolete] The name of the resource group within Microsoft Azure DNS.");
        }
    }
}
