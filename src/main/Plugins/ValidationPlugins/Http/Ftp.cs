using PKISharp.WACS.Clients;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class Ftp : HttpValidation<FtpOptions, Ftp>
    {
        private FtpClient _ftpClient;

        public Ftp(FtpOptions options, HttpValidationParameters pars) : base(options, pars)
        {
            _ftpClient = new FtpClient(_options.Credential, pars.LogService);
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
