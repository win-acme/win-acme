using PKISharp.WACS.Services;

namespace PKISharp.WACS
{
    public class AzureDnsOptions
    {
        public string ClientId { get; set; }
        public string ResourceGroupName { get; set; }
        public string Secret { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }

        public AzureDnsOptions() { }

        public AzureDnsOptions(IOptionsService options)
        {
            TenantId = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureTenantId);
            ClientId = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureClientId);
            Secret = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureSecret);
            SubscriptionId = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureSubscriptionId);
            ResourceGroupName = options.TryGetRequiredOption(nameof(options.Options.AzureTenantId), options.Options.AzureResourceGroupName);
        }

        public AzureDnsOptions(IOptionsService options, IInputService input)
        {
            TenantId = options.TryGetOption(options.Options.AzureTenantId, input, "Tenant Id");
            ClientId = options.TryGetOption(options.Options.AzureClientId, input, "Client Id");
            Secret = options.TryGetOption(options.Options.AzureSecret, input, "Secret", true);
            SubscriptionId = options.TryGetOption(options.Options.AzureSubscriptionId, input, "DNS Subscription ID");
            ResourceGroupName = options.TryGetOption(options.Options.AzureResourceGroupName, input, "DNS Resoure Group Name");
        }
    }
}