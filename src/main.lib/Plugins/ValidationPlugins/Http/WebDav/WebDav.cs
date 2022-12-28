using PKISharp.WACS.Client;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [IPlugin.Plugin<WebDavOptions, WebDavOptionsFactory>
        ("7e191d0e-30d1-47b3-ae2e-442499d33e16", "WebDav", "Upload verification files via WebDav")]
    internal class WebDav : HttpValidation<WebDavOptions, WebDav>
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

        protected override async Task DeleteFile(string path) => _webdavClient.Delete(path);

        protected override async Task DeleteFolder(string path) => _webdavClient.Delete(path);

        protected override async Task<bool> IsEmpty(string path) => !_webdavClient.IsEmpty(path);

        protected override char PathSeparator => '/';

        protected override async Task WriteFile(string path, string content) => _webdavClient.Upload(path, content);
        public override async Task CleanUp()
        {
            await base.CleanUp();
            _webdavClient.Dispose();
        }
    }
}
