using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class WebDavOptions : HttpValidationOptions<WebDav>
    {
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
            if (Credential != null)
            {
                Credential.Show(input);
            }
        }
    }
}
