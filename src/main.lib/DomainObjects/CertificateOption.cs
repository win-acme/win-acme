namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Represents a certificate
    /// </summary>
    internal class CertificateOption
    {
        public ICertificateInfo WithPrivateKey { get; set; }
        public ICertificateInfo WithoutPrivateKey { get; set; }

        public CertificateOption(ICertificateInfo withPrivateKey, ICertificateInfo withoutPrivateKey)
        {
            WithPrivateKey = withPrivateKey;
            WithoutPrivateKey = withoutPrivateKey;
        }
    }
}
