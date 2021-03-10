namespace PKISharp.WACS.Plugins.Azure.Common
{
    public class AzureArgumentsCommon
    {
        public string AzureEnvironment { get; set; }
        public bool AzureUseMsi { get; set; }
        public string AzureTenantId { get; set; }
        public string AzureClientId { get; set; }
        public string AzureSecret { get; set; }
    }
}