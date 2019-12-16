namespace PKISharp.WACS.Services.Legacy
{
    internal class LegacyAzureDnsOptions
    {
        public string? ClientId { get; set; }
        public string? ResourceGroupName { get; set; }
        public string? Secret { get; set; }
        public string? SubscriptionId { get; set; }
        public string? TenantId { get; set; }
    }
}
