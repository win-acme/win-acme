using System;
using System.IO;
using System.Net;

namespace LetsEncrypt.ACME.Simple
{
    public class FTPPlugin : ManualPlugin
    {
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

        public override string Name => "FTP";

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

        public void Upload(string ftpPath, string content)
        {
            Uri ftpUri = new Uri(ftpPath);
            Program.Log.Debug("ftpUri {@ftpUri}", ftpUri);
            EnsureDirectories(ftpUri);
            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Program.Log.Debug("Using SSL");
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + ftpUri.AbsolutePath;
            Program.Log.Debug("ftpConnection {@ftpConnection}", ftpConnection);

            Program.Log.Debug("UserName {@UserName}", FtpCredentials.UserName);

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpConnection);

            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = FtpCredentials;

            if (ftpUri.Scheme == "ftps")
            {
                request.EnableSsl = true;
                request.UsePassive = true;
            }

            Stream requestStream = request.GetRequestStream();
            stream.CopyTo(requestStream);
            requestStream.Close();

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                Program.Log.Information("Upload Status {StatusDescription}", response.StatusDescription);
        }

        public void EnsureDirectories(Uri ftpUri)
        {
            string[] directories = ftpUri.AbsolutePath.Split('/');

            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Program.Log.Debug("Using SSL");
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + "/";
            Program.Log.Debug("ftpConnection {@ftpConnection}", ftpConnection);

            Program.Log.Debug("UserName {@UserName}", FtpCredentials.UserName);

            if (directories.Length > 1)
            {
                for (int i = 1; i < (directories.Length - 1); i++)
                {
                    ftpConnection = ftpConnection + directories[i] + "/";
                    FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpConnection);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    request.Credentials = FtpCredentials;

                    if (ftpUri.Scheme == "ftps")
                    {
                        request.EnableSsl = true;
                        request.UsePassive = true;
                    }

                    try
                    {
                        FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                        Stream ftpStream = response.GetResponseStream();

                        ftpStream.Close();
                        response.Close();
                    }
                    catch (Exception ex)
                    {
                        Program.Log.Warning("Error creating FTP directory {@ex}", ex);
                    }
                }
            }
        }

        public string GetFiles(string ftpPath)
        {
            Uri ftpUri = new Uri(ftpPath);
            Program.Log.Debug("ftpUri {@ftpUri}", ftpUri);
            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Program.Log.Debug("Using SSL");
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + ftpUri.AbsolutePath;
            Program.Log.Debug("ftpConnection {@ftpConnection}", ftpConnection);

            Program.Log.Debug("UserName {@UserName}", FtpCredentials.UserName);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpConnection);

            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = FtpCredentials;

            if (ftpUri.Scheme == "ftps")
            {
                request.EnableSsl = true;
                request.UsePassive = true;
            }

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);
            string names = reader.ReadToEnd();

            reader.Close();
            response.Close();

            Program.Log.Debug("Files {@names}", names);
            return names.TrimEnd('\r', '\n');
        }

        public void Delete(string ftpPath, FileType fileType)
        {
            Uri ftpUri = new Uri(ftpPath);
            Program.Log.Debug("ftpUri {@ftpUri}", ftpUri);
            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Program.Log.Debug("Using SSL");
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + ftpUri.AbsolutePath;
            Program.Log.Debug("ftpConnection {@ftpConnection}", ftpConnection);

            Program.Log.Debug("UserName {@UserName}", FtpCredentials.UserName);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpConnection);

            if (fileType == FileType.File)
            {
                request.Method = WebRequestMethods.Ftp.DeleteFile;
            }
            else if (fileType == FileType.Directory)
            {
                request.Method = WebRequestMethods.Ftp.RemoveDirectory;
            }
            request.Credentials = FtpCredentials;

            if (ftpUri.Scheme == "ftps")
            {
                request.EnableSsl = true;
                request.UsePassive = true;
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                Program.Log.Information("Delete Status {StatusDescription}", response.StatusDescription);
        }

        public enum FileType
        {
            File,
            Directory
        }
    }
}