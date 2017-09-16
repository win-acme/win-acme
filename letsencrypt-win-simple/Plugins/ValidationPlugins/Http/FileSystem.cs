using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ACMESharp.ACME;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class FileSystem : HttpValidation
    {
        public override string Name
        {
            get
            {
                return "Http-FileSystem";
            }
        }

        public override void WriteFile(string root, string path, string content)
        {
            throw new NotImplementedException();
        }
    }
}
