using ACMESharp;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Services;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class FtpFactory : BaseValidationPluginFactory<Ftp>
    {
        public FtpFactory() :
            base(nameof(Ftp),
            "Upload verification file to FTP(S) server",
            AcmeProtocol.CHALLENGE_TYPE_HTTP) {}

        public override bool CanValidate(Target target) => string.IsNullOrEmpty(target.WebRootPath) || target.WebRootPath.StartsWith("ftp");
    }

    class Ftp : HttpValidation
    {
        private FtpClient _ftpClient;

        public Ftp(ScheduledRenewal target, ILogService logService, IInputService inputService, ProxyService proxyService) : 
            base(logService, inputService, proxyService, target)
        {
            _ftpClient = new FtpClient(target.Binding.HttpFtpOptions, logService);
        }

        public override char PathSeparator => '/';

        public override void DeleteFile(string path)
        {
            _ftpClient.Delete(path, FtpClient.FileType.File);
        }

        public override void DeleteFolder(string path)
        {
            _ftpClient.Delete(path, FtpClient.FileType.Directory);
        }

        public override bool IsEmpty(string path)
        {
            return _ftpClient.GetFiles(path).Count() == 0;
        }

        public override void WriteFile(string path, string content)
        {
            _ftpClient.Upload(path, content);
        }

        public override void Default(Target target, IOptionsService optionsService)
        {
            base.Default(target, optionsService);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = optionsService.TryGetRequiredOption(nameof(optionsService.Options.WebRoot), optionsService.Options.WebRoot);
            }
            target.HttpFtpOptions = new FtpOptions(optionsService);
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
        {
            base.Aquire(target, optionsService, inputService);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = optionsService.TryGetOption(optionsService.Options.WebRoot, _input, new[] {
                    "Enter a site path (the web root of the host for http authentication)",
                    " Example, ftp://domain.com:21/site/wwwroot/",
                    " Example, ftps://domain.com:990/site/wwwroot/"
                });
            }
            target.HttpFtpOptions = new FtpOptions(optionsService, _input);
        }
    }
}
