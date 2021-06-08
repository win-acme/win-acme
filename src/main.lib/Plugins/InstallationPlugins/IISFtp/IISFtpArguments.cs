using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISFtpArguments : BaseArguments
    {
        public override string Name => "IIS FTP plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation iisftp";

        [CommandLine(Description = "Site id to install certificate to.")]
        public long? FtpSiteId { get; set; }
    }
}
