namespace LetsEncrypt.ACME.Simple.Plugins.StorePlugins
{
    interface IStorePlugin : IHasName
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

        /// <summary>
        /// Search persisted storage for certificate with matching friendlyName
        /// </summary>
        /// <param name="thumbPrint"></param>
        /// <returns></returns>
        CertificateInfo FindByFriendlyName(string friendlyName);
    }
}
