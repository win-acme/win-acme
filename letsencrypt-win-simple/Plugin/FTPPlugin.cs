using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Serilog;

namespace LetsEncrypt.ACME.Simple
{
    internal class FTPPlugin : Plugin
    {
        private string hostName;

        private string ftpPath;

        public NetworkCredential FtpCredentials { get; set; }

        public override string Name => "FTP";

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

            Console.WriteLine("Enter a site path (the web root of the host where files will be uploaded for http authentication)");
            Console.WriteLine("Example, ftp://domain.com:21/site/wwwroot/");
            Console.WriteLine("Example, ftps://domain.com:990/site/wwwroot/");
            Console.Write(": ");
            ftpPath = Console.ReadLine();

            Console.Write("Enter the FTP username: ");
            string ftpUser = Console.ReadLine();

            Console.Write("Enter the FTP password: ");
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
            Console.WriteLine(" F: Generate a certificate via FTP/FTPS and install it manually.");
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
                    Log.Information("You can find the certificate at {pfxFilename}", pfxFilename);
                }
            }
            else
            {
                Log.Error("The FTP Credentials are not set. Please specify them and try again.");
            }
            return pfxFilename;
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information("Writing challenge answer to {answerPath}", answerPath);
            Upload(answerPath, fileContents);
        }

        private void EnsureDirectories(Uri ftpUri)
        {
            string[] directories = ftpUri.AbsolutePath.Split('/');

            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Log.Debug("Using SSL");
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + "/";
            Log.Debug("ftpConnection {@ftpConnection}", ftpConnection);

            Log.Debug("UserName {@UserName}", FtpCredentials.UserName);

            if (directories.Length > 1)
            {
                for (int i = 1; i < (directories.Length - 1); i++)
                {
                    ftpConnection = ftpConnection + directories[i] + "/";
                    if (!ExistsDir(new Uri(ftpConnection)))
                    {
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
                            Log.Warning("Error creating FTP directory {@ex}", ex);
                        }
                    }
                }
            }
        }

        private bool ExistsDir(Uri ftpUri)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUri);
            request.Method = WebRequestMethods.Ftp.ListDirectory;
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
            catch
            {
                return false;
            }
            return true;
        }

        private void Upload(string ftpPath, string content)
        {
            Uri ftpUri = new Uri(ftpPath);
            Log.Debug("ftpUri {@ftpUri}", ftpUri);
            EnsureDirectories(ftpUri);
            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Log.Debug("Using SSL");
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + ftpUri.AbsolutePath;
            Log.Debug("ftpConnection {@ftpConnection}", ftpConnection);

            Log.Debug("UserName {@UserName}", FtpCredentials.UserName);

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            FtpWebRequest request = (FtpWebRequest) WebRequest.Create(ftpConnection);

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
                Log.Information("Upload Status {StatusDescription}", response.StatusDescription);
        }

        private void Delete(string ftpPath, FileType fileType)
        {
            Uri ftpUri = new Uri(ftpPath);
            Log.Debug("ftpUri {@ftpUri}", ftpUri);
            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Log.Debug("Using SSL");
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + ftpUri.AbsolutePath;
            Log.Debug("ftpConnection {@ftpConnection}", ftpConnection);

            Log.Debug("UserName {@UserName}", FtpCredentials.UserName);

            FtpWebRequest request = (FtpWebRequest) WebRequest.Create(ftpConnection);

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
                Log.Information("Delete Status {StatusDescription}", response.StatusDescription);
        }

        private string GetFiles(string ftpPath)
        {
            Uri ftpUri = new Uri(ftpPath);
            Log.Debug("ftpUri {@ftpUri}", ftpUri);
            var scheme = ftpUri.Scheme;
            if (ftpUri.Scheme == "ftps")
            {
                scheme = "ftp";
                Log.Debug("Using SSL");
            }
            string ftpConnection = scheme + "://" + ftpUri.Host + ":" + ftpUri.Port + ftpUri.AbsolutePath;
            Log.Debug("ftpConnection {@ftpConnection}", ftpConnection);

            Log.Debug("UserName {@UserName}", FtpCredentials.UserName);

            FtpWebRequest request = (FtpWebRequest) WebRequest.Create(ftpConnection);

            request.Method = WebRequestMethods.Ftp.ListDirectory;
            request.Credentials = FtpCredentials;

            if (ftpUri.Scheme == "ftps")
            {
                request.EnableSsl = true;
                request.UsePassive = true;
            }
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
            
            Log.Information("Writing web.config to add extensionless mime type to {webConfigPath}", webConfigPath);
            string webConfigSourceFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");
            Upload(webConfigPath, File.ReadAllText(webConfigSourceFile));
        }

        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Log.Information("Deleting answer");
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
                            Log.Information("Deleting web.config");
                            Delete(folderPath + "web.config", FileType.File);
                            Log.Information("Deleting {folderPath}", folderPath);
                            Delete(folderPath, FileType.Directory);
                            int index = filePath.IndexOf("/");
                            var filePathFirstDirectory =
                                Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath.Replace("\\", "/"), filePath.Remove(index, (filePath.Length - index))));
                            Log.Information("Deleting {filePathFirstDirectory}", filePathFirstDirectory);
                            Delete(filePathFirstDirectory, FileType.Directory);
                        }
                        else
                        {
                            Log.Warning("Additional files exist in {folderPath} not deleting.", folderPath);
                        }
                    }
                    else
                    {
                        Log.Warning("Additional files exist in {folderPath} not deleting.", folderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
            }
        }

        private enum FileType
        {
            File,
            Directory
        }
    }
}