using PKISharp.WACS.Clients;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class Sftp : HttpValidation<SftpOptions, Sftp>
    {
        private readonly SshFtpClient _sshFtpClient;

        public Sftp(SftpOptions options, HttpValidationParameters pars, RunLevel runLevel) : base(options, runLevel, pars) => _sshFtpClient = new SshFtpClient(_options.Credential?.GetCredential(), pars.LogService);

        protected override char PathSeparator => '/';

        protected override void DeleteFile(string path) => _sshFtpClient.Delete(path, SshFtpClient.FileType.File);

        protected override void DeleteFolder(string path) => _sshFtpClient.Delete(path, SshFtpClient.FileType.Directory);

        protected override bool IsEmpty(string path) => !_sshFtpClient.GetFiles(path).Any();

        protected override void WriteFile(string path, string content) => _sshFtpClient.Upload(path, content);

        public override Task CleanUp()
        {
            base.CleanUp();
            return Task.CompletedTask;
        }
    }
}
