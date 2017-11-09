using ACMESharp;
using LetsEncrypt.ACME.Simple.Client;
using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Services;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class WebDavFactory : BaseValidationPluginFactory<WebDav>
    {
        public WebDavFactory() : 
            base(nameof(WebDav), 
                "Upload verification file to WebDav path", 
                AcmeProtocol.CHALLENGE_TYPE_HTTP) { }

        public override bool CanValidate(Target target) => string.IsNullOrEmpty(target.WebRootPath) || target.WebRootPath.StartsWith("\\\\");
    }

    class WebDav : HttpValidation
    {
        private WebDavClient _webdavClient;

        public WebDav(ScheduledRenewal target, ILogService logService,  IInputService inputService, IOptionsService optionsService, ProxyService proxyService) : 
            base(logService, inputService, proxyService)
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

        public override void Default(Target target, IOptionsService optionsService)
        {
            base.Default(target, optionsService);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = optionsService.TryGetRequiredOption(nameof(optionsService.Options.WebRoot), optionsService.Options.WebRoot);
            }
            target.HttpWebDavOptions = new WebDavOptions(optionsService);
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
        {
            base.Aquire(target, optionsService, inputService);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = optionsService.TryGetOption(optionsService.Options.WebRoot, _input, new[] {
                     "Enter a site path (the web root of the host for http authentication)",
                    " Example, http://domain.com:80/",
                    " Example, https://domain.com:443/"
                });
            }
            target.HttpWebDavOptions = new WebDavOptions(optionsService, _input);
        }
    }
}
