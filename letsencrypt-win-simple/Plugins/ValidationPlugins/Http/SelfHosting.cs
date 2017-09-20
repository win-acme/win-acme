using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class SelfHosting : HttpValidation, IDisposable
    {
        public override string Name => nameof(SelfHosting);
        public override string Description => "Self-host verification files (port 80 has to be available)";
        public HttpListener Listener { get; private set; }
        public Dictionary<string, string> Files { get; private set; }
        public Task ListeningTask { get; private set; }

        public SelfHosting()
        {
            Files = new Dictionary<string, string>();
            Listener = new HttpListener();
            Listener.Prefixes.Add($"http://+:80/");
            Listener.Start();
            ListeningTask = Task.Run(RecieveRequests);
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

        public override void DeleteFile(string path) {}
        public override void DeleteFolder(string path) {}
        public override bool IsEmpty(string path) => true;
        public override void WriteFile(string path, string content) => Files.Add(path, content);
        public override string CombinePath(string root, string path) => path;
        public override bool CanValidate(Target target) => target.IIS == false;

        public void Dispose()
        {
            if (Listener != null)
            {
                Listener.Stop();
                Listener = null;
            }
        }
    }
}
