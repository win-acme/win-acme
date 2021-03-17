using PKISharp.WACS.Plugins.Azure.Common;
using System;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    public class AzureArguments : AzureArgumentsCommon
    {
        public string AzureResourceGroupName { get; set; }
        public string AzureSubscriptionId { get; set; }
    }
}