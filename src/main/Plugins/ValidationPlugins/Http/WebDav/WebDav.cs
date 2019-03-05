using PKISharp.WACS.Client;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class WebDav : HttpValidation<WebDavOptions, WebDav>
    {
        private WebDavClient _webdavClient;

        public WebDav(WebDavOptions options, HttpValidationParameters pars) : base(options, pars)
        {
            _webdavClient = new WebDavClient(_options.Credential, pars.LogService);
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
            return !_webdavClient.GetFiles(path).Any();
        }

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
