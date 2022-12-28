using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISOptions : InstallationPluginOptions<IIS>
    {
        public long? SiteId { get; set; }
        public string? NewBindingIp { get; set; }
        public int? NewBindingPort { get; set; }

        public override string Name => "IIS";
        public override string Description => "Create or update bindings in IIS";
    }
}
