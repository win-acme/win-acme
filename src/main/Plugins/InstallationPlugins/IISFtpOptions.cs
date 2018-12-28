using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpOptions : InstallationPluginOptions<IISFtp>
    {
        public long SiteId { get; set; }
        public override string Name => "IISFTP";
        public override string Description => "Create or update ftps bindings in IIS";
    }
}
