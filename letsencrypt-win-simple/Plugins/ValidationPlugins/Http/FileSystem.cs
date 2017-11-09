using ACMESharp;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Extensions;
using System;
using System.IO;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class FileSystemFactory : BaseValidationPluginFactory<FileSystem>
    {
        public FileSystemFactory() :
            base(nameof(FileSystem),
             "Save file on local (network) path",
            AcmeProtocol.CHALLENGE_TYPE_HTTP) { }
    }

    class FileSystem : HttpValidation
    {
        public FileSystem(ScheduledRenewal target, ILogService logService, IInputService inputService, ProxyService proxyService) : 
            base(logService, inputService, proxyService)
        {
            if (target.Binding.PluginName != IISSitesFactory.SiteServer && !target.Binding.WebRootPath.ValidPath(logService))
            {
                throw new ArgumentException(nameof(target.Binding.WebRootPath));
            }
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

        public override void Default(Target target, IOptionsService optionsService)
        {
            base.Default(target, optionsService);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = optionsService.TryGetRequiredOption(nameof(optionsService.Options.WebRoot), optionsService.Options.WebRoot);
                if (!target.WebRootPath.ValidPath(_log))
                {
                    throw new ArgumentException(nameof(optionsService.Options.WebRoot));
                }
            }
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
        {
            base.Aquire(target, optionsService, inputService);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                do
                {
                    target.WebRootPath = optionsService.TryGetOption(optionsService.Options.WebRoot, _input, "Enter a site path (the web root of the host for http authentication)");
                }
                while (!target.WebRootPath.ValidPath(_log));
            }
        }
    }
}
