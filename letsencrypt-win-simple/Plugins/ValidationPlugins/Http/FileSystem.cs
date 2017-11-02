using ACMESharp.ACME;
using System.IO;
using System.Linq;
using System;
using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Clients;
using System.Text.RegularExpressions;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class FileSystem : HttpValidation
    {
        public override string Name => nameof(FileSystem);
        public override string Description => "Save file on local (network) path";

        private IISClient _iisClient = new IISClient();
 
        public override IValidationPlugin CreateInstance(Target target)
        {
            if (target.PluginName != IISSiteServerPlugin.PluginName && !Valid(target.WebRootPath) )
            {
                throw new ArgumentException();
            }
            return new FileSystem();
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

        public override void Default(IOptionsService options, Target target)
        {
            base.Default(options, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetRequiredOption(nameof(options.Options.WebRoot), options.Options.WebRoot);
                if (!Valid(target.WebRootPath))
                {
                    throw new ArgumentException(nameof(options.Options.WebRoot));
                }
            }
        }

        public override void Aquire(IOptionsService options, IInputService input, Target target)
        {
            base.Aquire(options, input, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                do
                {
                    target.WebRootPath = options.TryGetOption(options.Options.WebRoot, input, "Enter a site path (the web root of the host for http authentication)");
                }
                while (!Valid(target.WebRootPath));
            }
        }

        private bool Valid(string path)
        {
            try
            {
                var fi = new DirectoryInfo(path);
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
