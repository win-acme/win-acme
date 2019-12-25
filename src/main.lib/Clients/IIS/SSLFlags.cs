using System;

namespace PKISharp.WACS.Clients.IIS
{
    /// <summary>
    /// Desired flags for binding, closely matching the respective flags
    /// that IIS actually has. Needed because IIS7 is missing this feature.
    /// </summary>
    [Flags]
    public enum SSLFlags
    {
        None = 0,
        SNI = 1,
        CentralSsl = 2,
        DisableHttp2 = 4,
        DisableOcspStapling = 8,
        DisableQuic = 16,
        DisableTls13OverTcp = 32,
        DisableLegacyTls = 64,

        /// <summary>
        /// Flags introduced in specific versions of Windows
        /// </summary>
        IIS10_Flags = IIS10_Server2016_Flags | IIS10_Server2019_Flags,
        IIS10_Server2016_Flags = DisableHttp2 | DisableOcspStapling,
        IIS10_Server2019_Flags = DisableLegacyTls | DisableTls13OverTcp | DisableQuic,

        /// <summary>
        /// Incompatibiliy between certain flags
        /// </summary>
        NotWithCentralSsl = DisableHttp2 | DisableOcspStapling | DisableQuic | DisableTls13OverTcp | DisableLegacyTls
    }
}
