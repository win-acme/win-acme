using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class Ftp : HttpValidation<FtpOptions, Ftp>
    {
        private FtpClient _ftpClient;

        public Ftp(ScheduledRenewal renewal, TargetPart target, FtpOptions options, ILogService log, IInputService input, ProxyService proxy, string identifier) : 
            base(log, input, options, proxy, renewal, target, identifier)
        {
            _ftpClient = new FtpClient(_options.Credential, log);
        }

        protected override char PathSeparator => '/';

        protected override void DeleteFile(string path)
        {
            _ftpClient.Delete(path, FtpClient.FileType.File);
        }

        protected override void DeleteFolder(string path)
        {
            _ftpClient.Delete(path, FtpClient.FileType.Directory);
        }

        protected override bool IsEmpty(string path)
        {
            return !_ftpClient.GetFiles(path).Any();
        }

        protected override void WriteFile(string path, string content)
        {
            _ftpClient.Upload(path, content);
        }

        public override void CleanUp()
        {
            _ftpClient = null;
            base.CleanUp();
        }
    }
}
