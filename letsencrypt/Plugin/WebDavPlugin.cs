using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Serilog;
using letsencrypt.Support;

namespace letsencrypt
{
    public class WebDAVPlugin : Plugin
    {
        private Dictionary<string, string> config;

        private string hostName;
        
        private string WebDAVPath;

        private NetworkCredential WebDAVCredentials { get; set; }

        public override string Name => R.WebDAV;

        public override bool RequiresElevated => false;

        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.W;

        public override bool Validate(Options options) => true;

        public override bool SelectOptions(Options options)
        {
            config = GetConfig(options);
            hostName = LetsEncrypt.GetString(config, "host_name");
            if (string.IsNullOrEmpty(hostName))
            {
                hostName = LetsEncrypt.PromptForText(options, R.Enterthesitehostname);
                RequireNotNull("host_name", hostName);
            }

            WebDAVPath = LetsEncrypt.GetString(config, "webdav_path");
            if (string.IsNullOrEmpty(WebDAVPath))
            {
                string message = R.EnterasitepathforWebDAVauthentication + "\n" +
                R.Example + ": http://example.com:80/" + "\n" +
                R.Example + ": https://example.com:443/" + "\n" +
                ": ";
                WebDAVPath = LetsEncrypt.PromptForText(options, message);
                RequireNotNull("webdav_path", WebDAVPath);
            }


            var WebDAVUser = LetsEncrypt.GetString(config, "webdav_user");
            if (string.IsNullOrEmpty(WebDAVUser))
            {
                WebDAVUser = LetsEncrypt.PromptForText(options, R.EntertheWebDAVusername);
                RequireNotNull("webdav_user", WebDAVUser);
            }

            var WebDAVPass = LetsEncrypt.GetString(config, "webdav_password");
            if (string.IsNullOrEmpty(WebDAVPass))
            {
                WebDAVPass = LetsEncrypt.PromptForText(options, R.EntertheWebDAVpassword);
            };

            WebDAVCredentials = new NetworkCredential(WebDAVUser, WebDAVPass);

            return true;
        }

        public override List<Target> GetTargets(Options options)
        {
            var result = new List<Target>();
            result.Add(new Target
            {
                Host = hostName,
                WebRootPath = WebDAVPath,
                PluginName = Name,
                AlternativeNames = AlternativeNames
            });
            return result;
        }

        public override void Renew(Target target, Options options)
        {
            Install(target, options);
        }

        public override void Install(Target target, Options options)
        {
            Auto(target, options);
        }

        public override void PrintMenu()
        {
            Console.WriteLine(R.WebDAVMenuOption);
        }
        
        public override string Auto(Target target, Options options)
        {
            string pfxFilename = null;
            if (WebDAVCredentials != null)
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
                Console.WriteLine(R.TheWebDAVcredentialsarenotset);
            }
            return pfxFilename;
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information(R.WritingchallengeanswertoanswerPath, answerPath);
            Upload(answerPath, fileContents);
        }

        private void Upload(string WebDAVPath, string content)
        {
            int pathLastSlash = WebDAVPath.LastIndexOf("/") + 1;
            string file = WebDAVPath.Substring(pathLastSlash);
            string path = WebDAVPath.Remove(pathLastSlash);

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            var webdavclient = CreateWebDAVClient(path);

            var fileUploaded = webdavclient.Upload("/", stream, file).Result;
            
            Log.Information(R.UploadStatusDescription, fileUploaded);
        }

        private async void Delete(string WebDAVPath)
        {
            var webdavclient = CreateWebDAVClient(WebDAVPath);

            try
            {
                await webdavclient.DeleteFile(webdavclient.BasePath);
            }
            catch (Exception ex)
            {
                Log.Warning(R.Errordeletingfileorfolder, ex);
            }

            string result = R.NA;
            
            Log.Information(R.DeleteStatusDescription, result);
        }

        private string GetFiles(string WebDAVPath)
        {
            var webdavclient = CreateWebDAVClient(WebDAVPath);

            var folderFiles = webdavclient.List().Result;
            string names = "";
            foreach (var file in folderFiles)
            {
                names = names + file.DisplayName + ",";
            }

            Log.Debug("Files {@names}", names);
            return names.TrimEnd('\r', '\n', ',');
        }

        private WebDAVClient.Client CreateWebDAVClient(string webDAVPath)
        {
            Uri WebDAVUri = new Uri(WebDAVPath);
            var scheme = WebDAVUri.Scheme;
            string WebDAVConnection = scheme + "://" + WebDAVUri.Host + ":" + WebDAVUri.Port;
            string path = WebDAVUri.AbsolutePath;

            var webdavclient = new WebDAVClient.Client(WebDAVCredentials);
            webdavclient.Server = WebDAVConnection;
            webdavclient.BasePath = path;
            return webdavclient;
        }

        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            answerPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
            var webConfigPath = Path.Combine(answerPath, "web.config");
            
            Log.Information(R.WritingWebConfig, webConfigPath);
            string webconfigxml = Path.Combine(BaseDirectory, "web_config.xml");
            Upload(webConfigPath, File.ReadAllText(webconfigxml));
        }

        public override void DeleteAuthorization(Options options, string answerPath, string token, string webRootPath, string filePath)
        {
            Log.Information(R.Deletinganswer);
            Delete(answerPath);

            try
            {
                if (options.CleanupFolders == true)
                {
                    var folderPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
                    var files = GetFiles(folderPath);

                    if (!string.IsNullOrWhiteSpace(files))
                    {
                        if (files == "web.config")
                        {
                            Log.Information(R.Deletingwebconfig);
                            Delete(folderPath + "web.config");
                            Log.Information(R.Deletingfolderpath, folderPath);
                            Delete(folderPath);
                            var filePathFirstDirectory =
                                Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath,
                                    filePath.Remove(filePath.IndexOf("/"), (filePath.Length - filePath.IndexOf("/")))));
                            Log.Information(R.Deletingfolderpath, filePathFirstDirectory);
                            Delete(filePathFirstDirectory);
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
    }
}