using LetsEncrypt.ACME.Simple.Client;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Services;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class WebDavFactory : HttpValidationFactory<WebDav>
    {
        public WebDavFactory() : base(nameof(WebDav), "Upload verification file to WebDav path") { }
        public override bool CanValidate(Target target) => string.IsNullOrEmpty(target.WebRootPath) || ValidateWebroot(target);
        public override bool ValidateWebroot(Target target) => target.WebRootPath.StartsWith("\\\\");

        public override string[] WebrootHint()
        {
            return new[] {
                "Enter a webdav path that leads to the web root of the host for http authentication",
                " Example, \\\\domain.com:80\\",
                " Example, \\\\domain.com:443\\"
            };
        }

        public override void Default(Target target, IOptionsService optionsService)
        {
            base.Default(target, optionsService);
            target.HttpWebDavOptions = new WebDavOptions(optionsService);
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
        {
            base.Aquire(target, optionsService, inputService);
            target.HttpWebDavOptions = new WebDavOptions(optionsService, inputService);
        }
    }

    /// <summary>
    /// WebDav validation
    /// </summary>
    class WebDav : HttpValidation
    {
        private WebDavClient _webdavClient;

        public WebDav(ScheduledRenewal target, ILogService logService,  IInputService inputService, IOptionsService optionsService, ProxyService proxyService) : 
            base(logService, inputService, proxyService, target)
        {
            _webdavClient = new WebDavClient(target.Binding.HttpWebDavOptions, logService);
        }

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
    }
}
