using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Azure.Common;
using System;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class AzureArguments : AzureArgumentsCommon
    {
        public override string Name => "Azure";
        public override string Group => "Validation";
        public override string Condition => "--validation azure";

        [CommandLine(Description = "Subscription ID to login into Microsoft Azure DNS.")]
        public string? AzureSubscriptionId { get; set; }

        [CommandLine(Description = "The name of the resource group within Microsoft Azure DNS.")]
        public string? AzureResourceGroupName { get; set; }

        [CommandLine(Description = "Hosted zone (blank to find best match)")]
        public string? AzureHostedZone { get; set; }
    }
}