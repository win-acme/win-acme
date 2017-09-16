using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;
using System.IO;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class FileSystemIIS : FileSystem
    {
        public override string Name
        {
            get
            {
                return "Http-FileSystem-IIS";
            }
        }

        public override void BeforeAuthorize(Options options, Target target, HttpChallenge challenge)
        {
            var x = new IISPlugin();
            x.UnlockSection("system.webServer/handlers");
            WriteFile(target.WebRootPath, challenge.FilePath.Replace(challenge.Token, "web.config"), File.ReadAllText(_templateWebConfig));
        }

        public override void BeforeDelete(Options options, Target target, HttpChallenge challenge)
        {
            DeleteFile(target.WebRootPath, challenge.FilePath.Replace(challenge.Token, "web.config"));
        }
    }
}
