using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [Plugin("bc27d719-dcf2-41ff-bf08-54db7ea49c48")]
    internal class FtpOptions : HttpValidationOptions<Ftp>
    {
        public override string Name => "FTP";
        public override string Description => "Upload verification files via FTP(S)";

        public FtpOptions() : base() { }
        public FtpOptions(HttpValidationOptions<Ftp> source) : base(source) { }

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
