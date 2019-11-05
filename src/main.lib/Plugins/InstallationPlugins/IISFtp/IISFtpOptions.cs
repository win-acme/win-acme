using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("13058a79-5084-48af-b047-634e0ee222f4")]
    internal class IISFtpOptions : InstallationPluginOptions<IISFtp>
    {
        public long SiteId { get; set; }
        public override string Name => "IISFTP";
        public override string Description => "Create or update ftps bindings in IIS";
    }
}
