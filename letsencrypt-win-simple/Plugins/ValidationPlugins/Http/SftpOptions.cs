using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SftpOptions : BaseHttpValidationOptions<Sftp>
    {
        public override string Name { get => "SFTP"; }
        public override string Description { get => "Upload verification files via SSH-FTP"; }

        public SftpOptions() : base() { }
        public SftpOptions(BaseHttpValidationOptions<Sftp> source) : base(source) { }

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
