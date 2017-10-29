using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class SelfHosting : HttpValidation
    {
        public override string Name => nameof(SelfHosting);
        public override string Description => "Self-host verification files (port 80 will be unavailable during validation)";
        public HttpListener Listener { get; private set; }
        public Dictionary<string, string> Files { get; private set; }
        public Task ListeningTask { get; private set; }

        public SelfHosting() {}

        public SelfHosting(Target target)
        {
            Files = new Dictionary<string, string>();
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://+:80/");
            Listener.Start();
            ListeningTask = Task.Run(RecieveRequests);
        }

        public override IValidationPlugin CreateInstance(Target target)
        {
            return new SelfHosting(target);
        }

        public async Task RecieveRequests()
        {
             while (Listener.IsListening)
             {
                var ctx = await Listener.GetContextAsync();
                string response = null;
                var path = ctx.Request.Url.LocalPath.TrimStart('/');
                Files.TryGetValue(path, out response);
                using (var writer = new StreamWriter(ctx.Response.OutputStream))
                {
                    writer.Write(response);
                }
            }
        }

        public override void BeforeDelete(Target target, HttpChallenge challenge)
        {
            if (Listener != null)
            {
                Listener.Stop();
                Listener = null;
            }
        }
        
        public override void DeleteFile(string path) {}
        public override void DeleteFolder(string path) {}
        public override bool IsEmpty(string path) => true;
        public override void WriteFile(string path, string content) => Files.Add(path, content);
        public override string CombinePath(string root, string path) => path;
        public override bool CanValidate(Target target) => true;
        public override void Aquire(IOptionsService options, InputService input, Target target) {}
        public override void Default(IOptionsService options, Target target) {}
    }
}
