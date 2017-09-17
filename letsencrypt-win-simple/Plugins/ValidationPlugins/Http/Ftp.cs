using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Services;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class Ftp : HttpValidation
    {
        private FTPPlugin FtpPlugin = new FTPPlugin();

        public override string Description
        {
            get
            {
                return "Upload verification file to FTP(S) server";
            }
        }

        public override string Name
        {
            get
            {
                return nameof(Ftp);
            }
        }

        public override char PathSeparator => '/';

        public override void DeleteFile(string path)
        {
            FtpPlugin.Delete(path, FTPPlugin.FileType.File);
        }

        public override void DeleteFolder(string path)
        {
            FtpPlugin.Delete(path, FTPPlugin.FileType.Directory);
        }

        public override bool IsEmpty(string path)
        {
            return FtpPlugin.GetFiles(path).Count() == 0;
        }

        public override void WriteFile(string path, string content)
        {
            FtpPlugin.Upload(path, content);
        }

        public override bool CanValidate(Target target)
        {
            return string.IsNullOrEmpty(target.WebRootPath) || target.WebRootPath.StartsWith("ftp");
        }

        public override void Default(Options options, Target target)
        {
            base.Default(options, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetRequiredOption(nameof(options.WebRoot), options.WebRoot);
            }
            target.FtpOptions = new FtpOptions(options);
        }

        public override void Aquire(Options options, InputService input, Target target)
        {
            base.Aquire(options, input, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetOption(options.WebRoot, input, new[] {
                    "Enter a site path (the web root of the host for http authentication)",
                    " Example, ftp://domain.com:21/site/wwwroot/",
                    " Example, ftps://domain.com:990/site/wwwroot/"
                });
            }
            target.FtpOptions = new FtpOptions(options, input);
        }
    }
}
