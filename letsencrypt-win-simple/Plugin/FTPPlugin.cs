using System;
using System.IO;
using System.Net;

namespace LetsEncrypt.ACME.Simple
{
    public class FTPPlugin : ManualPlugin
    {
        private NetworkCredential FtpCredentials { get; set; }

        public override string Name => "FTP";

        public override void Renew(Target target)
        {
            Program.Log.Warning("Renewal is not supported for the FTP Plugin.");
        }

        public override string MenuOption => "F";
        public override string Description => "Generate a certificate via FTP/ FTPS and install it manually.";

        public override void Run()
        {
            var target = InputTarget(Name, new[] {
                "Enter a site path (the web root of the host for http authentication)",
                " Example, ftp://domain.com:21/site/wwwroot/",
                " Example, ftps://domain.com:990/site/wwwroot/"
            });
            if (target != null)
            {
                var ftpUser = Program.Input.RequestString("Enter the FTP username");
                var ftpPass = Program.Input.ReadPassword("Enter the FTP password");
                FtpCredentials = new NetworkCredential(ftpUser, ftpPass);
                Auto(target);
            }
        }

        public override void Auto(Target target)
        {
            if (FtpCredentials != null)
            {
                var auth = Program.Authorize(target);
                if (auth.Status == "valid")
                {
                    var pfxFilename = Program.GetCertificate(target);
                    Program.Log.Information("You can find the certificate at {pfxFilename}", pfxFilename);
                }
            }
            else
            {
                Program.Log.Error("The FTP Credentials are not set. Please specify them and try again.");
            }
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Program.Log.Debug("Writing challenge answer to {answerPath}", answerPath);
            Upload(answerPath, fileContents);
        }

        private void EnsureDirectories(Uri ftpUri)
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

        private void Upload(string ftpPath, string content)
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

        private void Delete(string ftpPath, FileType fileType)
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

        private string GetFiles(string ftpPath)
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

        private readonly string _sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");

        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            answerPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
            var webConfigPath = Path.Combine(answerPath, "web.config");

            Program.Log.Debug("Writing web.config to add extensionless mime type to {webConfigPath}", webConfigPath);

            Upload(webConfigPath, File.ReadAllText(_sourceFilePath));
        }

        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Program.Log.Verbose("Deleting answer");
            Delete(answerPath, FileType.File);

            try
            {
                if (Properties.Settings.Default.CleanupFolders == true)
                {
                    var folderPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
                    var files = GetFiles(folderPath);

                    if (!string.IsNullOrWhiteSpace(files))
                    {
                        if (files == "web.config")
                        {
                            Program.Log.Debug("Deleting web.config");
                            Delete(folderPath + "web.config", FileType.File);
                            Program.Log.Debug("Deleting {folderPath}", folderPath);
                            Delete(folderPath, FileType.Directory);
                            var filePathFirstDirectory =
                                Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath,
                                    filePath.Remove(filePath.IndexOf("/"), (filePath.Length - filePath.IndexOf("/")))));
                            Program.Log.Debug("Deleting {filePathFirstDirectory}", filePathFirstDirectory);
                            Delete(filePathFirstDirectory, FileType.Directory);
                        }
                        else
                        {
                            Program.Log.Warning("Additional files exist in {folderPath}, not deleting.", folderPath);
                        }
                    }
                    else
                    {
                        Program.Log.Warning("Additional files exist in {folderPath}, not deleting.", folderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
            }
        }

        private enum FileType
        {
            File,
            Directory
        }

    }
}