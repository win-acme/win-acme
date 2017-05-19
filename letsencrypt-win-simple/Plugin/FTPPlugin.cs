using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Serilog;

namespace LetsEncrypt.ACME.Simple
{
    internal class FTPPlugin : Plugin
    {
        private string hostName;

        private string ftpPath;

        public NetworkCredential FtpCredentials { get; set; }

        public override string Name => R.FTP;

        public override bool RequiresElevated => false;
        
        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.F;

        public override bool Validate() => true;

        public override List<Target> GetTargets()
        {
            var result = new List<Target>();
            result.Add(new Target
            {
                Host = hostName,
                WebRootPath = ftpPath,
                PluginName = Name,
                AlternativeNames = AlternativeNames
            });
            return result;
        }

        public override bool SelectOptions(Options options)
        {
            Console.Write("Enter a host name: ");
            hostName = Console.ReadLine();

            Console.WriteLine(R.EnterSitePath);
            Console.WriteLine(R.Example + ": ftp://domain.com:21/site/wwwroot/");
            Console.WriteLine(R.Example + ": ftps://domain.com:990/site/wwwroot/");
            Console.Write(": ");
            ftpPath = Console.ReadLine();

            Console.Write(R.EntertheFTPusername);
            string ftpUser = Console.ReadLine();

            Console.Write(R.EntertheFTPpassword);
            var ftpPass = LetsEncrypt.ReadPassword();

            FtpCredentials = new NetworkCredential(ftpUser, ftpPass);
            return !string.IsNullOrEmpty(ftpPath) && !string.IsNullOrEmpty(hostName);
        }

        public override void Install(Target target, Options options)
        {
            Auto(target, options);
        }

        public override void Renew(Target target, Options options)
        {
            Install(target, options);
        }

        public override void PrintMenu()
        {
            Console.WriteLine(R.FTPMenuOption);
        }

        public override string Auto(Target target, Options options)
        {
            string pfxFilename = null;
            if (FtpCredentials != null)
            {
                pfxFilename = base.Auto(target, options);
                if (!string.IsNullOrEmpty(pfxFilename))
                { 
                    Console.WriteLine("");
                    Log.Information(R.YoucanfindthecertificateatpfxFilename, pfxFilename);
                }
            }
            else
            {
                Log.Error(R.TheFTPCredentialsarenotset);
            }
            return pfxFilename;
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information(R.WritingchallengeanswertoanswerPath, answerPath);
            Upload(answerPath, fileContents);
        }

        private string GetFTPConnectionString(Uri ftpUri)
        {
            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Log.Debug(R.UsingSSL);
            }
            return scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + "/";
        }

        private string GetFTPConnectionString(string ftpUri)
        {
            return GetFTPConnectionString(new Uri(ftpUri));
        }

        private FtpWebRequest CreateFTPRequest(string ftpConnection, string method)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpConnection);

            request.Method = method;
            request.Credentials = FtpCredentials;

            if (ftpConnection.StartsWith("ftps", StringComparison.InvariantCultureIgnoreCase))
            {
                request.EnableSsl = true;
                request.UsePassive = true;
            }
            return request;
        }

        private void EnsureDirectories(string ftpPath)
        {
            string[] directories = ftpPath.Split('/');

            string ftpConnection = GetFTPConnectionString(ftpPath);

            if (directories.Length > 1)
            {
                for (int i = 1; i < (directories.Length - 1); i++)
                {
                    ftpConnection = ftpConnection + directories[i] + "/";
                    if (!ExistsDir(ftpConnection))
                    {
                        FtpWebRequest request = CreateFTPRequest(ftpConnection, WebRequestMethods.Ftp.MakeDirectory);
                        try
                        {
                            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                            Stream ftpStream = response.GetResponseStream();

                            ftpStream.Close();
                            response.Close();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning(R.ErrorcreatingFTPdirectory, ex);
                        }
                    }
                }
            }
        }

        private bool ExistsDir(string ftpConnection)
        {
            FtpWebRequest request = CreateFTPRequest(ftpConnection, WebRequestMethods.Ftp.ListDirectory);

            try
            {
                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Stream ftpStream = response.GetResponseStream();

                ftpStream.Close();
                response.Close();
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void Upload(string ftpPath, string content)
        {
            EnsureDirectories(ftpPath);
            string ftpConnection = GetFTPConnectionString(ftpPath);

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            FtpWebRequest request = CreateFTPRequest(ftpConnection, WebRequestMethods.Ftp.UploadFile);

            Stream requestStream = request.GetRequestStream();
            stream.CopyTo(requestStream);
            requestStream.Close();

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Log.Information(R.UploadStatusDescription, response.StatusDescription);
            }
        }

        private void Delete(string ftpPath, FileType fileType)
        {
            string ftpConnection = GetFTPConnectionString(ftpPath);

            FtpWebRequest request = CreateFTPRequest(ftpConnection, 
                fileType == FileType.File
                ? WebRequestMethods.Ftp.DeleteFile
                : WebRequestMethods.Ftp.RemoveDirectory);

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                Log.Information(R.DeleteStatusDescription, response.StatusDescription);
            }
        }

        private string GetFiles(string ftpPath)
        {
            string ftpConnection = GetFTPConnectionString(ftpPath);

            FtpWebRequest request = CreateFTPRequest(ftpConnection, WebRequestMethods.Ftp.ListDirectory);

            string names = "";
            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream))
                    {
                        names = reader.ReadToEnd();
                    }
                }
            }

            Log.Debug("Files {@names}", names);
            return names.TrimEnd('\r', '\n');
        }
        
        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            answerPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
            var webConfigPath = Path.Combine(answerPath, "web.config");
            
            Log.Information(R.WritingWebConfig, webConfigPath);
            string webConfigSourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");
            Upload(webConfigPath, File.ReadAllText(webConfigSourceFile));
        }

        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Log.Information(R.Deletinganswer);
            Delete(answerPath, FileType.File);

            try
            {
                if (Properties.Settings.Default.CleanupFolders == true)
                {
                    var folderPath = answerPath.Remove((answerPath.Length - token.Length), token.Length).Replace("\\", "/");
                    var files = GetFiles(folderPath);

                    if (!string.IsNullOrWhiteSpace(files))
                    {
                        if (files == "web.config")
                        {
                            Log.Information(R.Deletingwebconfig);
                            Delete(folderPath + "web.config", FileType.File);
                            Log.Information(R.Deletingfolderpath, folderPath);
                            Delete(folderPath, FileType.Directory);
                            int index = filePath.IndexOf("/");
                            var filePathFirstDirectory =
                                Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath.Replace("\\", "/"), filePath.Remove(index, (filePath.Length - index))));
                            Log.Information(R.Deletingfolderpath, filePathFirstDirectory);
                            Delete(filePathFirstDirectory, FileType.Directory);
                        }
                        else
                        {
                            Log.Warning(R.Additionalfilesexistinfolderpath, folderPath);
                        }
                    }
                    else
                    {
                        Log.Warning(R.Additionalfilesexistinfolderpath, folderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning(R.Erroroccuredwhiledeletingfolderstructure, ex);
            }
        }

        private enum FileType
        {
            File,
            Directory
        }
    }
}