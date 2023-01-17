using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    /// <summary>
    /// Do not make INull, we actually need to store these options to override
    /// the default behaviour of CertificateStore
    /// </summary>
    internal class NullOptions : StorePluginOptions {}

}
