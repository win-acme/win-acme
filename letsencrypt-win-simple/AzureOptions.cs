namespace LetsEncrypt.ACME.Simple
{
    public class AzureOptions
    {
        public string ClientId { get; set; }
        public string ResourceGroupName { get; set; }
        public string Secret { get; set; }
        public string SubscriptionId { get; set; }
        public string TenantId { get; set; }

        internal static AzureOptions From(Options options)
        {
            if (options == null
                || string.IsNullOrWhiteSpace(options.AzureClientId)
                || string.IsNullOrWhiteSpace(options.AzureResourceGroupName)
                || string.IsNullOrWhiteSpace(options.AzureSecret)
                || string.IsNullOrWhiteSpace(options.AzureSubscriptionId)
                || string.IsNullOrWhiteSpace(options.AzureTenantId))
                return null;

            return new AzureOptions
            {
                ClientId = options.AzureClientId,
                ResourceGroupName = options.AzureResourceGroupName,
                Secret = options.AzureSecret,
                SubscriptionId = options.AzureSubscriptionId,
                TenantId = options.AzureTenantId
            };
        }

        internal void ApplyOn(Options options)
        {
            if (options == null)
                return;

            options.AzureClientId = ClientId;
            options.AzureResourceGroupName = ResourceGroupName;
            options.AzureSecret = Secret;
            options.AzureSubscriptionId = SubscriptionId;
            options.AzureTenantId = TenantId;
        }
    }
}