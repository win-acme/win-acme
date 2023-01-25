using PKISharp.WACS.DomainObjects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Does the actual work
    /// </summary>
    public interface IInstallationPlugin : IPlugin
    {
        /// <summary>
        /// Do the installation work
        /// </summary>
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="newCertificateInfo"></param>
        /// <param name="oldCertificateInfo"></param>
        Task<bool> Install(IEnumerable<Type> stores, ICertificateInfo newCertificateInfo, ICertificateInfo? oldCertificateInfo);
    }
}
