using ACMESharp;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Services;
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
        public FileSystem(ScheduledRenewal target, ILogService logService, IInputService inputService, IOptionsService optionsService) : base(logService, inputService, optionsService)
        {
            if (target.Binding.PluginName != IISSitesFactory.SiteServer && !Valid(target.Binding.WebRootPath))
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

        public override void Default(Target target)
        {
            base.Default(target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = _options.TryGetRequiredOption(nameof(_options.Options.WebRoot), _options.Options.WebRoot);
                if (!Valid(target.WebRootPath))
                {
                    throw new ArgumentException(nameof(_options.Options.WebRoot));
                }
            }
        }

        public override void Aquire(Target target)
        {
            base.Aquire(target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                do
                {
                    target.WebRootPath = _options.TryGetOption(_options.Options.WebRoot, _input, "Enter a site path (the web root of the host for http authentication)");
                }
                while (!Valid(target.WebRootPath));
            }
        }

        private bool Valid(string path)
        {
            try
            {
                var fi = new DirectoryInfo(CombinePath(path, ""));
                if (!fi.Exists)
                {
                    _log.Error("Directory {path} does not exist", fi.FullName);
                    return false;
                }
                return true;
            }
            catch
            {
                _log.Error("Unable to parse path {path}", path);
                return false;
            }
        }
    }
}
