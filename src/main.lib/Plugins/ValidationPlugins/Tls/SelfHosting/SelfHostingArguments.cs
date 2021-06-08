using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Tls
{
    internal class SelfHostingArguments : BaseArguments
    {
        public override string Name => "SelfHosting plugin";
        public override string Group => "Validation";
        public override string Condition => "--validationmode tls-alpn-01 --validation selfhosting";
        public override bool Default => true;

        [CommandLine(Description = "Port to use for listening to validation requests. Note that the ACME server will always send requests to port 443. This option is only useful in combination with a port forwarding.")]
        public int? ValidationPort { get; set; }
    }
}
