using System;
using System.IO;
using System.Net;

namespace LetsEncrypt.ACME.Simple
{
    public class FTPPlugin : ManualPlugin
    {
        public const string PluginName = "FTP";

        private NetworkCredential FtpCredentials {
            get
            {
                if (_FtpCredentials == null)
                {
                    var ftpUser = Program.Input.RequestString("Enter the FTP username");
                    var ftpPass = Program.Input.ReadPassword("Enter the FTP password");
                    _FtpCredentials = new NetworkCredential(ftpUser, ftpPass);
                }
                return _FtpCredentials;
            }
        }
        private NetworkCredential _FtpCredentials;

        public override string Name => PluginName;

        public override void Renew(Target target)
        {
            Program.Log.Warning("Renewal is not supported for the FTP Plugin.");
        }

        public override string MenuOption => "F";
        public override string Description => "Generate a certificate via FTP(S) and install it manually.";

        public override void Run()
        {
            var target = InputTarget(Name, new[] {
                "Enter a site path (the web root of the host for http authentication)",
                " Example, ftp://domain.com:21/site/wwwroot/",
                " Example, ftps://domain.com:990/site/wwwroot/"
            });
            if (target != null) {
                Auto(target);
            }
        }

        public override void Auto(Target target)
        {
            var auth = Program.Authorize(target);
            if (auth.Status == "valid")
            {
                var pfxFilename = Program.GetCertificate(target);
                Program.Log.Information("You can find the certificate at {pfxFilename}", pfxFilename);
            }
        }

        private FtpWebRequest CreateRequest(string ftpPath)
        {
            Uri ftpUri = new Uri(ftpPath);
            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + ftpUri.AbsolutePath;
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpConnection);
            request.Credentials = FtpCredentials;
            if (ftpUri.Scheme == "ftps")
            {
                request.EnableSsl = true;
                request.UsePassive = true;
            }
            return request;
        }

        public void Upload(string ftpPath, string content)
        {
            EnsureDirectories(ftpPath);
            using (MemoryStream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                writer.Write(content);
                writer.Flush();
                stream.Position = 0;

                var request = CreateRequest(ftpPath);
                request.Method = WebRequestMethods.Ftp.UploadFile;

                using (Stream requestStream = request.GetRequestStream())
                {
                    stream.CopyTo(requestStream);
                }
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    Program.Log.Verbose("Upload {ftpPath} status {StatusDescription}", ftpPath, response.StatusDescription?.Trim());
                }
            }
        }

        private void EnsureDirectories(string ftpPath)
        {
            var ftpUri = new Uri(ftpPath);
            string[] directories = ftpUri.AbsolutePath.Split('/');
            string path = ftpUri.Scheme + "://" + ftpUri.Host + ":" + (ftpUri.Port == -1 ? 21 : ftpUri.Port) + "/";
            if (directories.Length > 1)
            {
                for (int i = 1; i < (directories.Length - 1); i++)
                {
                    path = path + directories[i] + "/";
                    var request = CreateRequest(path);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    try
                    {
                        using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                        {
                            Program.Log.Verbose("Create {ftpPath} status {StatusDescription}", ftpPath, response.StatusDescription?.Trim());
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Log.Verbose("Create {ftpPath} failed, may already exist ({Message})", ftpPath, ex.Message);
                    }
                }
            }
        }

        public string GetFiles(string ftpPath)
        {
            var request = CreateRequest(ftpPath);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
            string names;
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            using (Stream responseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(responseStream))
            {
                names = reader.ReadToEnd();
            }
            Program.Log.Verbose("Files in path {ftpPath}: {@names}", ftpPath, names);
            return names.TrimEnd('\r', '\n');
        }

        public void Delete(string ftpPath, FileType fileType)
        {
            var request = CreateRequest(ftpPath);
            if (fileType == FileType.File)
            {
                request.Method = WebRequestMethods.Ftp.DeleteFile;
            }
            else if (fileType == FileType.Directory)
            {
                request.Method = WebRequestMethods.Ftp.RemoveDirectory;
            }
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Program.Log.Verbose("Delete {ftpPath} status {StatusDescription}", ftpPath, response.StatusDescription?.Trim());
            }
        }

        public enum FileType
        {
            File,
            Directory
        }
    }
}