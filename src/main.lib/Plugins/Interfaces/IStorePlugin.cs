using PKISharp.WACS.DomainObjects;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IStorePlugin
    {
        /// <summary>
        /// Perist certificate and update CertificateInfo
        /// </summary>
        /// <param name="certificateInfo"></param>
        Task Save(CertificateInfo certificateInfo);

        /// <summary>
        /// Remove certificate from persisted storage
        /// </summary>
        /// <param name="certificateInfo"></param>
        Task Delete(CertificateInfo certificateInfo);
    }
}
