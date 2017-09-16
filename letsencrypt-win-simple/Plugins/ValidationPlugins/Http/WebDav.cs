using ACMESharp.ACME;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class WebDav : HttpValidation
    {
        private WebDavPlugin WebDavPlugin = new WebDavPlugin();

        public override string Name
        {
            get
            {
                return "Http-WebDav";
            }
        }

        public override void BeforeAuthorize(Options options, Target target, HttpChallenge challenge)
        {
            WriteFile(target.WebRootPath, challenge.FilePath.Replace(challenge.Token, "web.config"), File.ReadAllText(_templateWebConfig));
        }

        public override void BeforeDelete(Options options, Target target, HttpChallenge challenge)
        {
            DeleteFile(target.WebRootPath, challenge.FilePath.Replace(challenge.Token, "web.config"));
        }
    }
}
