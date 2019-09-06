using PKISharp.WACS.Client;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class WebDav : HttpValidation<WebDavOptions, WebDav>
    {
        private WebDavClientWrapper _webdavClient;

        public WebDav(
            WebDavOptions options, HttpValidationParameters pars, 
            RunLevel runLevel, ProxyService proxy) : 
            base(options, runLevel, pars)
        {
            _webdavClient = new WebDavClientWrapper(_options.Credential, pars.LogService, proxy);
        }

        protected override void DeleteFile(string path)
        {
            _webdavClient.Delete(path);
        }

        protected override void DeleteFolder(string path)
        {
            _webdavClient.Delete(path);
        }

        protected override bool IsEmpty(string path)
        {
            return !_webdavClient.IsEmpty(path);
        }

        protected override char PathSeparator => '/';

        protected override void WriteFile(string path, string content)
        {
            _webdavClient.Upload(path, content);
        }
        public override void CleanUp()
        {
            base.CleanUp();
            _webdavClient = null;
        }
    }
}
