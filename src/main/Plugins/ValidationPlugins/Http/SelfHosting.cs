using ACMESharp.Authorizations;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class SelfHosting : BaseValidation<SelfHostingOptions, Http01ChallengeValidationDetails>
    {
        internal const int DefaultValidationPort = 80;

        private HttpListener _listener;
        private Dictionary<string, string> _files;
        private readonly Task _listeningTask;


        public SelfHosting(string identifier, ILogService log, SelfHostingOptions options) : 
            base(log, options, identifier)
        {
            try
            {
                var prefix = $"http://+:{options.Port ?? DefaultValidationPort}/.well-known/acme-challenge/";
                _files = new Dictionary<string, string>();
                _listener = new HttpListener();
                _listener.Prefixes.Add(prefix);
                _listener.Start();
                _listeningTask = Task.Run(RecieveRequests);
            }
            catch
            {
                _log.Error("Unable to activate HttpListener, this may be due to non-Microsoft webserver using port 80");
                throw;
            }
        }

        public async Task RecieveRequests()
        {
            while (_listener.IsListening)
            {
                var ctx = await _listener.GetContextAsync();
                string response = null;
                var path = ctx.Request.Url.LocalPath;
                if (_files.TryGetValue(path, out response))
                {
                    _log.Verbose("SelfHosting plugin serving file {name}", path);
                    using (var writer = new StreamWriter(ctx.Response.OutputStream))
                    {
                        writer.Write(response);
                    }
                }
                else
                {
                    _log.Warning("SelfHosting plugin couldn't serve file {name}", path);
                    ctx.Response.StatusCode = 404;
                }
            }
        }
        
        public override void CleanUp()
        {
            _listener.Stop();
            _listener.Close();
            _listener = null;
        }

        public override void PrepareChallenge()
        {
            _files.Add("/" + _challenge.HttpResourcePath, _challenge.HttpResourceValue);
        }
    }
}
