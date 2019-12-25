using Fclp;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using System.Net;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISWebArgumentsProvider : BaseArgumentsProvider<IISWebArguments>
    {
        private const string SslPortParameterName = "sslport";
        private const string SslIpParameterName = "sslipaddress";

        public override string Name => "IIS Web plugin";
        public override string Group => "Installation";
        public override string Condition => "--installation iis";

        public override bool Validate(ILogService log, IISWebArguments current, MainArguments main)
        {
            if (!base.Validate(log, current, main))
            {
                return false;
            }
            if (!string.IsNullOrEmpty(current.SSLPort))
            {
                if (int.TryParse(current.SSLPort, out var port))
                {
                    if (port < 1 || port > 65535)
                    {
                        log.Error("Invalid --{param}, value should be between 1 and 65535", SslPortParameterName);
                        return false;
                    }
                }
                else
                {
                    log.Error("Invalid --{param}, value should be a number", SslPortParameterName);
                    return false;
                }
            }
            if (!string.IsNullOrEmpty(current.SSLIPAddress))
            {
                if (!IPAddress.TryParse(current.SSLIPAddress, out _))  
                {
                    log.Error("Invalid --{sslipaddress}", SslIpParameterName);
                    return false;
                }
            }
            return true;
        }

        public override void Configure(FluentCommandLineParser<IISWebArguments> parser)
        {
            parser.Setup(o => o.InstallationSiteId)
                .As("installationsiteid")
                .WithDescription("Specify site to install new bindings to. Defaults to the target if that is an IIS site.");
            parser.Setup(o => o.SSLPort)
                .As(SslPortParameterName)
                .WithDescription($"Port number to use for newly created HTTPS bindings. Defaults to {IISClient.DefaultBindingPort}.");
            parser.Setup(o => o.SSLIPAddress)
                .As(SslIpParameterName)
                .WithDescription($"IP address to use for newly created HTTPS bindings. Defaults to {IISClient.DefaultBindingIp}.");
        }
    }
}
