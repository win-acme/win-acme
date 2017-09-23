using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple
{
    public class DnsAzureOptions
    {
        public string ClientId { get; set; }
        public string ResourceGroupName { get; set; }
        public string Secret { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }

        public DnsAzureOptions() { }

        public DnsAzureOptions(Options options)
        {
            TenantId = options.TryGetRequiredOption(nameof(options.AzureTenantId), options.AzureTenantId);
            ClientId = options.TryGetRequiredOption(nameof(options.AzureTenantId), options.AzureClientId);
            Secret = options.TryGetRequiredOption(nameof(options.AzureTenantId), options.AzureSecret);
            SubscriptionId = options.TryGetRequiredOption(nameof(options.AzureTenantId), options.AzureSubscriptionId);
            ResourceGroupName = options.TryGetRequiredOption(nameof(options.AzureTenantId), options.AzureResourceGroupName);
        }

        public DnsAzureOptions(Options options, InputService input)
        {
            TenantId = options.TryGetOption(options.AzureTenantId, input, "Tenant Id");
            ClientId = options.TryGetOption(options.AzureClientId, input, "Client Id");
            Secret = options.TryGetOption(options.AzureSecret, input, "Secret", true);
            SubscriptionId = options.TryGetOption(options.AzureSubscriptionId, input, "DNS Subscription ID");
            ResourceGroupName = options.TryGetOption(options.AzureResourceGroupName, input, "DNS Resoure Group Name");
        }
    }
}