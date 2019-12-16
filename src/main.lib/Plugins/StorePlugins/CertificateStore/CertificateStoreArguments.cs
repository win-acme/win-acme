namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStoreArguments
    {
        public bool KeepExisting { get; set; }
        public string? CertificateStore { get; set; }
        public string? AclFullControl { get; set; }
    }
}
