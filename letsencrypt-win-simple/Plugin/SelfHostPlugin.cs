using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple
{
    public sealed class SelfHostPlugin : ManualPlugin, IDisposable
    {
        public HttpListener Listener { get; private set; }
        public Dictionary<string, string> Files { get; private set; }
        public Task ListeningTask { get; private set; }

        public override string Name => "Selfhost";
        public override bool UseEmptyPath => true;

        public override void PrintMenu()
        {
            Console.WriteLine(" S: Selfhost the response files.");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "s")
            {
                Files = new Dictionary<string, string>();

                Console.Write("Enter a port to listen on: ");
                var port = int.Parse(Console.ReadLine());
                var path = $"http://+:{port}/";

                Listener = new HttpListener();
                Listener.Prefixes.Add(path);
                Listener.Start();

                ListeningTask = Task.Run(RecieveRequests);

                Console.WriteLine($"Listening to requests matching {path}");
                base.HandleMenuResponse("m", targets);
            }
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

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Files.Add(answerPath, fileContents);
        }

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
