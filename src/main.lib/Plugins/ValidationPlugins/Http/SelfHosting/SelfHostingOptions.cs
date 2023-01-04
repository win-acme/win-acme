using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHostingOptions : ValidationPluginOptions
    {
        /// <summary>
        /// Alternative port for validation. Note that ACME always requires
        /// port 80 to be open. This is only useful if the port is interally 
        /// mapped/forwarded to a different one.
        /// </summary>
        public int? Port { get; set; }

        /// <summary>
        /// Default would be http, but may be set to https
        /// </summary>
        public bool? Https { get; set; }

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
            if (Https == true)
            {
                input.Show("Protocol", "https");
            }
        }
    }
}
