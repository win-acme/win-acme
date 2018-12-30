using System;

namespace PKISharp.WACS.Clients.IIS
{
    [Flags]
    public enum SSLFlags
    {
        None = 0,
        SNI = 1,
        CentralSSL = 2
    }
}
