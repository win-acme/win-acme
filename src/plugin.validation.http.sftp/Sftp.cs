using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [IPlugin.Plugin<
        SftpOptions, SftpOptionsFactory,
        HttpValidationCapability, SftpJson>
        ("048aa2e7-2bce-4d3e-b731-6e0ed8b8170d",
        "SFTP", "Upload verification files via SSH-FTP")]
    public class Sftp : HttpValidation<SftpOptions>
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

        protected override Task DeleteFile(string path)
        {
            _sshFtpClient.Delete(path, SshFtpClient.FileType.File);
            return Task.CompletedTask;
        } 

        protected override Task DeleteFolder(string path)
        {
            _sshFtpClient.Delete(path, SshFtpClient.FileType.Directory);
            return Task.CompletedTask;
        }

        protected override Task<bool> IsEmpty(string path)
        {
            return Task.FromResult(!_sshFtpClient.GetFiles(path).Any());
        }

        protected override Task WriteFile(string path, string content)
        {
            _sshFtpClient.Upload(path, content);
            return Task.CompletedTask;
        }
    }
}
