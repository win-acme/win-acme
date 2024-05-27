using PKISharp.WACS.Client;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [IPlugin.Plugin<
        WebDavOptions, WebDavOptionsFactory, 
        HttpValidationCapability, WebDavJson>
        ("7e191d0e-30d1-47b3-ae2e-442499d33e16",
        "WebDav", "Upload verification files via WebDav")]
    public class WebDav : HttpValidation<WebDavOptions>
    {
        private readonly WebDavClientWrapper _webdavClient;

        public WebDav(
            WebDavOptions options, 
            HttpValidationParameters pars,
            RunLevel runLevel, 
            IProxyService proxy,
            SecretServiceManager secretService) :
            base(options, runLevel, pars) => 
            _webdavClient = new WebDavClientWrapper(
                _options.Credential, 
                pars.LogService, 
                proxy, 
                secretService);

        protected override Task DeleteFile(string path) 
        { 
            _webdavClient.Delete(path); 
            return Task.CompletedTask; 
        }

        protected override Task DeleteFolder(string path)
        {
            _webdavClient.Delete(path);
            return Task.CompletedTask;
        }

        protected override Task<bool> IsEmpty(string path)
        {
            return Task.FromResult(_webdavClient.IsEmpty(path));
        }

        protected override char PathSeparator => '/';

        protected override Task WriteFile(string path, string content)
        {
            _webdavClient.Upload(path, content);
            return Task.CompletedTask;
        }

        public override async Task CleanUp()
        {
            await base.CleanUp();
            _webdavClient.Dispose();
        }
    }
}
