using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SftpOptions : HttpValidationOptions<Sftp>
    {
        public SftpOptions() : base() { }
        public SftpOptions(HttpValidationOptions<Sftp> source) : base(source) { }

        /// <summary>
        /// Credentials to use for SFTP connection
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
