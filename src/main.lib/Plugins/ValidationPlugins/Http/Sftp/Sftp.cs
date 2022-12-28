using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [IPlugin.Plugin<SftpOptions, SftpOptionsFactory, WacsJson>
        ("048aa2e7-2bce-4d3e-b731-6e0ed8b8170d", "SFTP", "Upload verification files via SSH-FTP")]
    internal class Sftp : HttpValidation<SftpOptions, Sftp>
    {
        private readonly SshFtpClient _sshFtpClient;

        public Sftp(
            SftpOptions options, 
            HttpValidationParameters pars,
            RunLevel runLevel, 
            SecretServiceManager secretService) :
            base(options, runLevel, pars) => 
            _sshFtpClient = new SshFtpClient(
                _options.Credential?.GetCredential(secretService), 
                pars.LogService);

        protected override char PathSeparator => '/';

        protected override async Task DeleteFile(string path) => _sshFtpClient.Delete(path, SshFtpClient.FileType.File);

        protected override async Task DeleteFolder(string path) => _sshFtpClient.Delete(path, SshFtpClient.FileType.Directory);

        protected override async Task<bool> IsEmpty(string path) => !_sshFtpClient.GetFiles(path).Any();

        protected override async Task WriteFile(string path, string content) => _sshFtpClient.Upload(path, content);
    }
}
