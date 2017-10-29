using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Services;
using System.Linq;
using System;
using LetsEncrypt.ACME.Simple.Clients;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class Ftp : HttpValidation
    {
        public Ftp() { }
        public Ftp(Target target)
        {
            _ftpClient = new FtpClient(target.HttpFtpOptions);
        }

        private FtpClient _ftpClient { get; set; }
        public override string Name => nameof(Ftp);
        public override string Description => "Upload verification file to FTP(S) server";
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

        public override bool CanValidate(Target target)
        {
            return string.IsNullOrEmpty(target.WebRootPath) || target.WebRootPath.StartsWith("ftp");
        }

        public override void Default(IOptionsService options, Target target)
        {
            base.Default(options, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetRequiredOption(nameof(options.Options.WebRoot), options.Options.WebRoot);
            }
            target.HttpFtpOptions = new FtpOptions(options);
        }

        public override void Aquire(IOptionsService options, IInputService input, Target target)
        {
            base.Aquire(options, input, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetOption(options.Options.WebRoot, input, new[] {
                    "Enter a site path (the web root of the host for http authentication)",
                    " Example, ftp://domain.com:21/site/wwwroot/",
                    " Example, ftps://domain.com:990/site/wwwroot/"
                });
            }
            target.HttpFtpOptions = new FtpOptions(options, input);
        }

        public override IValidationPlugin CreateInstance(Target target)
        {
            return new Ftp(target);
        }
    }
}
