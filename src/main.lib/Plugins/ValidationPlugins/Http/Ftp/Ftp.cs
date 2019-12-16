using PKISharp.WACS.Clients;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class Ftp : HttpValidation<FtpOptions, Ftp>
    {
        private readonly FtpClient _ftpClient;

        public Ftp(FtpOptions options, HttpValidationParameters pars, RunLevel runLevel) : base(options, runLevel, pars) => _ftpClient = new FtpClient(_options.Credential, pars.LogService);

        protected override char PathSeparator => '/';

        protected override void DeleteFile(string path) => _ftpClient.Delete(path, FtpClient.FileType.File);

        protected override void DeleteFolder(string path) => _ftpClient.Delete(path, FtpClient.FileType.Directory);

        protected override bool IsEmpty(string path) => !_ftpClient.GetFiles(path).Any();

        protected override void WriteFile(string path, string content) => _ftpClient.Upload(path, content);

        public override Task CleanUp()
        {
            base.CleanUp();
            return Task.CompletedTask;
        }
    }
}
