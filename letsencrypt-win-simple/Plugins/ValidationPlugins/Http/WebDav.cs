using LetsEncrypt.ACME.Simple.Configuration;
using LetsEncrypt.ACME.Simple.Services;
using System;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class WebDav : HttpValidation
    {
        private WebDavPlugin WebDavPlugin = new WebDavPlugin();

        public override string Name
        {
            get
            {
                return nameof(WebDav);
            }
        }

        public override string Description
        {
            get
            {
                return "Upload verification file to WebDav path";
            }
        }

        public override void DeleteFile(string path)
        {
            WebDavPlugin.Delete(path);
        }

        public override void DeleteFolder(string path)
        {
            WebDavPlugin.Delete(path);
        }

        public override bool IsEmpty(string path)
        {
            return WebDavPlugin.GetFiles(path).Count() == 0;
        }

        public override void WriteFile(string path, string content)
        {
            WebDavPlugin.Upload(path, content);
        }

        public override bool CanValidate(Target target)
        {
            return string.IsNullOrEmpty(target.WebRootPath) || target.WebRootPath.StartsWith("\\\\");
        }

        public override void Default(Options options, Target target)
        {
            base.Default(options, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetRequiredOption(nameof(options.WebRoot), options.WebRoot);
            }
            target.WebDavOptions = new WebDavOptions(options);
        }

        public override void Aquire(Options options, InputService input, Target target)
        {
            base.Aquire(options, input, target);
            if (string.IsNullOrEmpty(target.WebRootPath))
            {
                target.WebRootPath = options.TryGetOption(options.WebRoot, input, new[] {
                     "Enter a site path (the web root of the host for http authentication)",
                    " Example, http://domain.com:80/",
                    " Example, https://domain.com:443/"
                });
            }
            target.WebDavOptions = new WebDavOptions(options, input);
        }
    }
}
