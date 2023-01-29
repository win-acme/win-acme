using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [IPlugin.Plugin<
        FtpOptions, FtpOptionsFactory, 
        HttpValidationCapability, WacsJsonPlugins>
        ("bc27d719-dcf2-41ff-bf08-54db7ea49c48",
        "FTP", "Upload verification files via FTP(S)")]
    internal class Ftp : HttpValidation<FtpOptions>
    {
        private readonly FtpClient _ftpClient;

        public Ftp(
            FtpOptions options, 
            HttpValidationParameters pars,
            RunLevel runLevel, 
            SecretServiceManager secretService) : 
            base(options, runLevel, pars) => 
            _ftpClient = new FtpClient(_options.Credential, pars.LogService, secretService);

        protected override char PathSeparator => '/';

        protected override Task DeleteFile(string path) => _ftpClient.DeleteFile(path);

        protected override Task DeleteFolder(string path) => _ftpClient.DeleteFolder(path);

        protected override async Task<bool> IsEmpty(string path) => !(await _ftpClient.GetFiles(path)).Any();

        protected override Task WriteFile(string path, string content) => _ftpClient.Upload(path, content);
    }
}
