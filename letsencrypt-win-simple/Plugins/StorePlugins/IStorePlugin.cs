using System;

namespace LetsEncrypt.ACME.Simple.Plugins.StorePlugins
{
    /// <summary>
    /// StorePluginFactory interface
    /// </summary>
    public interface IStorePluginFactory : IHasName, IHasType { }

    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class BaseStorePluginFactory<T> : BasePluginFactory<T>, IStorePluginFactory where T : IStorePlugin
    {
        public BaseStorePluginFactory(string name, string description) : base(name, description) { }
    }

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
