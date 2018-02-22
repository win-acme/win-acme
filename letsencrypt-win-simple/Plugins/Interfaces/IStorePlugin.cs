namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IStorePlugin
    {
        /// <summary>
        /// Perist certificate and update CertificateInfo
        /// </summary>
        /// <param name="certificateInfo"></param>
        void Save(CertificateInfo certificateInfo);

        /// <summary>
        /// Remove certificate from persisted storage
        /// </summary>
        /// <param name="certificateInfo"></param>
        void Delete(CertificateInfo certificateInfo);

        /// <summary>
        /// Search persisted storage for certificate with matching thumbprint
        /// </summary>
        /// <param name="thumbPrint"></param>
        /// <returns></returns>
        CertificateInfo FindByThumbprint(string thumbPrint);
    }
}
