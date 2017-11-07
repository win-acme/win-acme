using ACMESharp;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class FtpFactory : IValidationPluginFactory
    {
        public string Name => nameof(Ftp);
        public string Description => "Upload verification file to FTP(S) server";
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_HTTP;
        public bool CanValidate(Target target) => string.IsNullOrEmpty(target.WebRootPath) || target.WebRootPath.StartsWith("ftp");
        public Type Instance => typeof(Ftp);
    }

    class Ftp : HttpValidation
    {
        private FtpClient _ftpClient;

        public Ftp(Target target, ILogService logService, IInputService inputService, IOptionsService optionsService) : base(logService, inputService, optionsService)
        {
            _ftpClient = new FtpClient(target.HttpFtpOptions);
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

        public override void Default(Target target)
        {
            base.Default(target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = _options.TryGetRequiredOption(nameof(_options.Options.WebRoot), _options.Options.WebRoot);
            }
            target.HttpFtpOptions = new FtpOptions(_options);
        }

        public override void Aquire(Target target)
        {
            base.Aquire(target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = _options.TryGetOption(_options.Options.WebRoot, _input, new[] {
                    "Enter a site path (the web root of the host for http authentication)",
                    " Example, ftp://domain.com:21/site/wwwroot/",
                    " Example, ftps://domain.com:990/site/wwwroot/"
                });
            }
            target.HttpFtpOptions = new FtpOptions(_options, _input);
        }
    }
}
