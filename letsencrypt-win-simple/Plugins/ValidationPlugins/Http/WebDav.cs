using LetsEncrypt.ACME.Simple.Client;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class WebDav : HttpValidation
    {
        public WebDav() { }
        public WebDav(Target target)
        {
            _webdavClient = new WebDavClient(target.WebDavOptions);
        }

        private WebDavClient _webdavClient { get; set; }
        public override string Name => nameof(WebDav);
        public override string Description => "Upload verification file to WebDav path";
   
        public override void DeleteFile(string path)
        {
            _webdavClient.Delete(path);
        }

        public override void DeleteFolder(string path)
        {
            _webdavClient.Delete(path);
        }

        public override bool IsEmpty(string path)
        {
            return _webdavClient.GetFiles(path).Count() == 0;
        }

        public override void WriteFile(string path, string content)
        {
            _webdavClient.Upload(path, content);
        }

        public override bool CanValidate(Target target)
        {
            return string.IsNullOrEmpty(target.WebRootPath) || target.WebRootPath.StartsWith("\\\\");
        }

        public override void Default(Options options, Target target)
        {
            base.Default(options, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetRequiredOption(nameof(options.WebRoot), options.WebRoot);
            }
            target.WebDavOptions = new WebDavOptions(options);
        }

        public override void Aquire(Options options, InputService input, Target target)
        {
            base.Aquire(options, input, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetOption(options.WebRoot, input, new[] {
                     "Enter a site path (the web root of the host for http authentication)",
                    " Example, http://domain.com:80/",
                    " Example, https://domain.com:443/"
                });
            }
            target.WebDavOptions = new WebDavOptions(options, input);
        }

        public override IValidationPlugin CreateInstance(Target target)
        {
            return new WebDav(target);
        }
    }
}
