using Fclp;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class IISFtpArgumentsProvider : BaseArgumentsProvider<IISFtpArguments>
    {
        public override string Name => "IIS FTP";
        public override string Group => "Installation";
        public override string Condition => "--installation iisftp";

        public override void Configure(FluentCommandLineParser<IISFtpArguments> parser)
        {
            parser.Setup(o => o.FtpSiteId)
                .As("installationsiteid")
                .WithDescription("Specify site to install certificate to.");

            parser.Setup(o => o.FtpSiteId)
                .As("ftpsiteid")
                .WithDescription("Specify site to install certificate to.");
        }

        public override bool Active(IISFtpArguments current)
        {
            return current.FtpSiteId != null;
        }     
    }
}
