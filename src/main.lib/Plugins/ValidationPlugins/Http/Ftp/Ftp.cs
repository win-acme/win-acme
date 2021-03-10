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

        protected override async Task DeleteFile(string path) => _ftpClient.Delete(path, FtpClient.FileType.File);

        protected override async Task DeleteFolder(string path) => _ftpClient.Delete(path, FtpClient.FileType.Directory);

        protected override async Task<bool> IsEmpty(string path) => !_ftpClient.GetFiles(path).Any();

        protected override async Task WriteFile(string path, string content) => _ftpClient.Upload(path, content);
    }
}
