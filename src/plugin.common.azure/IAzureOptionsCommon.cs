using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.Azure.Common
{
    public interface IAzureOptionsCommon 
    {
        public string? AzureEnvironment { get; set; }
        public bool UseMsi { get; set; }
        public string? ClientId { get; set; }
        public ProtectedString? Secret { get; set; }
        public string? TenantId { get; set; }
    }
}
