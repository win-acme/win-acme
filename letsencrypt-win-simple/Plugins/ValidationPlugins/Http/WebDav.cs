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
                return "Http-WebDav";
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
    }
}
