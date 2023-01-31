namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Represents a certificate
    /// </summary>
    internal class CertificateOption
    {
        public CertificateInfo WithPrivateKey { get; set; }
        public CertificateInfo WithoutPrivateKey { get; set; }

        public CertificateOption(CertificateInfo withPrivateKey, CertificateInfo withoutPrivateKey)
        {
            WithPrivateKey = withPrivateKey;
            WithoutPrivateKey = withoutPrivateKey;
        }
    }
}
