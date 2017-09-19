using ACMESharp.ACME;
using System.IO;
using System.Linq;
using System;
using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Clients;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class FileSystem : HttpValidation
    {
        public override string Name => nameof(FileSystem);
        public override string Description => "Save file on local (network) path";
      
        public override void BeforeAuthorize(Target target, HttpChallenge challenge)
        {
            if (target.IIS == true)
            {
                var x = new IISClient();
                x.UnlockSection("system.webServer/handlers");
            }
            base.BeforeAuthorize(target, challenge);
        }

        public override void DeleteFile(string path)
        {
            (new FileInfo(path)).Delete();
        }

        public override void DeleteFolder(string path)
        {
            (new DirectoryInfo(path)).Delete();
        }

        public override bool IsEmpty(string path)
        {
            return (new DirectoryInfo(path)).GetFileSystemInfos().Count() == 0;
        }

        public override void WriteFile(string path, string content)
        {
            var fi = new FileInfo(path);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            File.WriteAllText(path, content);
        }

        public override void Default(Options options, Target target)
        {
            base.Default(options, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetRequiredOption(nameof(options.WebRoot), options.WebRoot);
            }
        }

        public override void Aquire(Options options, InputService input, Target target)
        {
            base.Aquire(options, input, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetOption(options.WebRoot, input, "Enter a site path (the web root of the host for http authentication)");
            }
        }
    }
}
