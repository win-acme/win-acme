using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class FtpOptions : HttpValidationOptions<Ftp>
    {
        public override string Name { get => "FTP"; }
        public override string Description { get => "Upload verification files via FTP(S)"; }

        public FtpOptions() : base() { }
        public FtpOptions(HttpValidationOptions<Ftp> source) : base(source) { }

        /// <summary>
        /// Credentials to use for WebDav connection
        /// </summary>
        public NetworkCredentialOptions Credential { get; set; }

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
