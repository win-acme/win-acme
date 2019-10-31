using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [Plugin("048aa2e7-2bce-4d3e-b731-6e0ed8b8170d")]
    internal class SftpOptions : HttpValidationOptions<Sftp>
    {
        public override string Name => "SFTP";
        public override string Description => "Upload verification files via SSH-FTP";

        public SftpOptions() : base() { }
        public SftpOptions(HttpValidationOptions<Sftp> source) : base(source) { }

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
