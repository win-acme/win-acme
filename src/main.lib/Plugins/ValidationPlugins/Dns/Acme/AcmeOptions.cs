using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class AcmeOptions : ValidationPluginOptions
    {
        public string? BaseUri { get; set; }
    }
}