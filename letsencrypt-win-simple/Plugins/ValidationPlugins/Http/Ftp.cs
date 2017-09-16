using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class Ftp : HttpValidation
    {
        private FTPPlugin FtpPlugin = new FTPPlugin();

        public override string Name
        {
            get
            {
                return "Http-Ftp";
            }
        }

        public override string PathSeparator => "/";

        public override void DeleteFile(string path)
        {
            FtpPlugin.Delete(path, FTPPlugin.FileType.File);
        }

        public override void DeleteFolder(string path)
        {
            FtpPlugin.Delete(path, FTPPlugin.FileType.Directory);
        }

        public override bool IsEmpty(string path)
        {
            return FtpPlugin.GetFiles(path).Count() == 0;
        }

        public override void WriteFile(string path, string content)
        {
            FtpPlugin.Upload(path, content);
        }
    }
}
