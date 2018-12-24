using PKISharp.WACS.Clients;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Sftp validation
    /// </summary>
    internal class SftpFactory : BaseHttpValidationFactory<Sftp>
    {
        public SftpFactory(ILogService log) : base(log, nameof(Sftp), "Upload verification file to SFTP server") {}

        public override bool CanValidate(Target target) => string.IsNullOrEmpty(target.WebRootPath) || ValidateWebroot(target);

        public override bool ValidateWebroot(Target target)
        {
            return target.WebRootPath.StartsWith("sftp");
        }

        public override string[] WebrootHint(bool allowEmpty)
        {
            return new[] {
                "Enter an sftp path that leads to the web root of the host for sftp authentication",
                    " Example, sftp://domain.com:22/site/wwwroot/"
                };
        }

        public override void Default(Target target, IOptionsService optionsService)
        {
            base.Default(target, optionsService);
            target.HttpFtpOptions = new FtpOptions(optionsService);
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            base.Aquire(target, optionsService, inputService, runLevel);
            target.HttpFtpOptions = new FtpOptions(optionsService, inputService);
        }
    }

    internal class Sftp : BaseHttpValidation
    {
        private SshFtpClient _sshFtpClient;

        public Sftp(ScheduledRenewal renewal, Target target, ILogService log, IInputService input, ProxyService proxy, string identifier) : 
            base(log, input, proxy, renewal, target, identifier)
        {
            _sshFtpClient = new SshFtpClient(target.HttpFtpOptions.GetCredential(), log);
        }

        protected override char PathSeparator => '/';

        protected override void DeleteFile(string path)
        {
            _sshFtpClient.Delete(path, SshFtpClient.FileType.File);
        }

        protected override void DeleteFolder(string path)
        {
            _sshFtpClient.Delete(path, SshFtpClient.FileType.Directory);
        }

        protected override bool IsEmpty(string path)
        {
            return !_sshFtpClient.GetFiles(path).Any();
        }

        protected override void WriteFile(string path, string content)
        {
            _sshFtpClient.Upload(path, content);
        }

        public override void CleanUp()
        {
            base.CleanUp();
            // Switched setting this to null, since this class will be needed for deleting files and folder structure
            _sshFtpClient = null;
        }
    }
}
