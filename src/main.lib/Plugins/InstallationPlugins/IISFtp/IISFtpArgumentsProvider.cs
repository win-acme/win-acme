using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpArgumentsProvider : BaseArgumentsProvider<IISFtpArguments>
    {
        public override string Name => "IIS FTP plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation iisftp";

        public override void Configure(FluentCommandLineParser<IISFtpArguments> parser)
        {
            parser.Setup(o => o.FtpSiteId)
                .As("ftpsiteid")
                .WithDescription("Site id to install certificate to.");
        }

        public override bool Active(IISFtpArguments current)
        {
            return current.FtpSiteId != null;
        }     
    }
}
