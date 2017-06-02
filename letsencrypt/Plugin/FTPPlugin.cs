using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Serilog;
using Newtonsoft.Json;
using letsencrypt.Support;

namespace letsencrypt
{
    public class FTPPlugin : Plugin
    {
        private Dictionary<string, string> config;

        private string hostName;

        private string ftpPath;

        public NetworkCredential FtpCredentials;

        public override string Name => R.FTP;

        public override bool RequiresElevated => false;
        
        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.F;

        public override bool Validate(Options options)
        {
            config = GetConfig(options);
            return true;
        }

        public override List<Target> GetTargets(Options options)
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
            hostName = LetsEncrypt.GetString(config, "host_name");
            if (string.IsNullOrEmpty(hostName))
            {
                hostName = LetsEncrypt.PromptForText(options, R.Enterhostname);
                RequireNotNull("host_name", hostName);
            }

            ftpPath = LetsEncrypt.GetString(config, "ftp_path");
            if (string.IsNullOrEmpty(ftpPath))
            {
                string message = R.EnterSitePath + "\n" +
                R.Example + ": ftp://domain.com:21/site/wwwroot/" + "\n" +
                R.Example + ": ftps://domain.com:990/site/wwwroot/" + "\n:";
                ftpPath = LetsEncrypt.PromptForText(options, message);
                RequireNotNull("host_name", ftpPath);
            }

            var ftpUser = LetsEncrypt.GetString(config, "ftp_user");
            if (string.IsNullOrEmpty(ftpUser))
            {
                ftpUser = LetsEncrypt.PromptForText(options, R.EntertheFTPusername);
                RequireNotNull("ftp_user", ftpUser);
            }
            
            var ftpPass = LetsEncrypt.GetString(config, "ftp_password");
            if (string.IsNullOrEmpty(ftpPass))
            {
                ftpPass = LetsEncrypt.PromptForText(options, R.EntertheFTPpassword);
                RequireNotNull("ftp_password", ftpPass);
            }

            FtpCredentials = new NetworkCredential(ftpUser, ftpPass);
            return !string.IsNullOrEmpty(ftpPath) && !string.IsNullOrEmpty(hostName);
        }
        
        public override void Install(Target target, Options options)
        {
            string pfxFilename = null;
            if (FtpCredentials != null)
            {
                pfxFilename = Auto(target, options);
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
        }

        public override void Renew(Target target, Options options)
        {
            Install(target, options);
        }

        public override void PrintMenu()
        {
            Console.WriteLine(R.FTPMenuOption);
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information(R.WritingchallengeanswertoanswerPath, answerPath);
            Upload(answerPath, fileContents);
        }

        private FtpWebRequest CreateFTPRequest(string ftpConnection, string method)
        {
            Uri ftpUri = new Uri(ftpConnection);
            var scheme = ftpUri.Scheme;
            var isSSL = ftpUri.Scheme == "ftps";
            if (isSSL)
            {
                scheme = "ftp";
            }
            var rootUri = $"{scheme}://{ftpUri.Host}:{ftpUri.Port}{ftpUri.AbsolutePath}";
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(rootUri);

            request.Method = method;
            request.Credentials = FtpCredentials;
            
            if (isSSL)
            {
                request.EnableSsl = true;
                request.UsePassive = true;
            }
            return request;
        }

        private void EnsureDirectories(string ftpPath)
        {
            string[] directories = ftpPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            Uri ftpUri = new Uri(ftpPath);
            string ftpConnection = ftpUri.Scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + "/";

            if (directories.Length > 1)
            {
                for (int i = 2; i < (directories.Length - 1); i++)
                {
                    if (!string.IsNullOrEmpty(directories[i]))
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

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            FtpWebRequest request = CreateFTPRequest(ftpPath, WebRequestMethods.Ftp.UploadFile);

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
            FtpWebRequest request = CreateFTPRequest(ftpPath, 
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
            FtpWebRequest request = CreateFTPRequest(ftpPath, WebRequestMethods.Ftp.ListDirectory);

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
            string webConfigSourceFile = Path.Combine(BaseDirectory, "web_config.xml");
            Upload(webConfigPath, File.ReadAllText(webConfigSourceFile));
        }

        public override void DeleteAuthorization(Options options, string answerPath, string token, string webRootPath, string filePath)
        {
            Log.Information(R.Deletinganswer);
            Delete(answerPath, FileType.File);

            try
            {
                if (options.CleanupFolders == true)
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