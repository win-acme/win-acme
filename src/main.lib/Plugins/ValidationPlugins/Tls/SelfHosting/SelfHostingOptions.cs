using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Tls
{
    [Plugin("a1565064-b208-4467-8ca1-1bd3c08aa500")]
    internal class SelfHostingOptions : ValidationPluginOptions<SelfHosting>
    {
        public override string ChallengeType => Constants.TlsAlpn01ChallengeType;
        public override string Name => "SelfHosting";
        public override string Description => "Answer TLS verification request from win-acme";

        /// <summary>
        /// Alternative port for validation. Note that ACME always requires
        /// port 80 to be open. This is only useful if the port is interally 
        /// mapped/forwarded to a different one.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Show to use what has been configured
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            if (Port != null)
            {
                input.Show("Port", Port.ToString());
            }
        }
    }
}
