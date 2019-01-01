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
        CentralSSL = 2
    }
}
