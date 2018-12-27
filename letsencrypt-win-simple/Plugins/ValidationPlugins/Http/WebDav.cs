using PKISharp.WACS.Client;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class WebDav : BaseHttpValidation<WebDavOptions, WebDav>
    {
        private WebDavClient _webdavClient;

        public WebDav(ScheduledRenewal renewal, Target target, WebDavOptions options, ILogService log, IInputService input, ProxyService proxy, string identifier) : 
            base(log, input, options, proxy, renewal, target, identifier)
        {
            _webdavClient = new WebDavClient(_options.Credential, log);
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
            _webdavClient = null;
            base.CleanUp();
        }
    }
}
