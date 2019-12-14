using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [Plugin("7e191d0e-30d1-47b3-ae2e-442499d33e16")]
    internal class WebDavOptions : HttpValidationOptions<WebDav>
    {
        public override string Name => "WebDav";
        public override string Description => "Upload verification files via WebDav";

        public WebDavOptions() : base() { }
        public WebDavOptions(HttpValidationOptions<WebDav> source) : base(source) { }

        /// <summary>
        /// Credentials to use for WebDav connection
        /// </summary>
        public NetworkCredentialOptions? Credential { get; set; }

        /// <summary>
        /// Show settings to user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            Credential.Show(input);
        }
    }
}
