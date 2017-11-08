using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class SelfHostingFactory : BaseValidationPluginFactory<SelfHosting>
    {
        public SelfHostingFactory() : 
            base(nameof(SelfHosting), 
                "Self-host verification files (port 80 will be unavailable during validation)",
                AcmeProtocol.CHALLENGE_TYPE_HTTP) { }
    }

    class SelfHosting : HttpValidation
    {
        private HttpListener _listener;
        public Dictionary<string, string> _files;
        private Task _listeningTask;

        public SelfHosting(ScheduledRenewal target, ILogService logService, IInputService inputService, IOptionsService optionsService) : base(logService, inputService, optionsService)
        {
            _files = new Dictionary<string, string>();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:80/");
            _listener.Start();
            _listeningTask = Task.Run(RecieveRequests);
        }

        public async Task RecieveRequests()
        {
             while (_listener.IsListening)
             {
                var ctx = await _listener.GetContextAsync();
                string response = null;
                var path = ctx.Request.Url.LocalPath.TrimStart('/');
                _files.TryGetValue(path, out response);
                using (var writer = new StreamWriter(ctx.Response.OutputStream))
                {
                    writer.Write(response);
                }
            }
        }

        public override void BeforeDelete(Target target, HttpChallenge challenge)
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener = null;
            }
        }
        
        public override void DeleteFile(string path) {}
        public override void DeleteFolder(string path) {}
        public override bool IsEmpty(string path) => true;
        public override void WriteFile(string path, string content) => _files.Add(path, content);
        public override string CombinePath(string root, string path) => path;
    }
}
