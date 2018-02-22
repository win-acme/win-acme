using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Net;
using Autofac;

namespace PKISharp.WACS.Clients
{
    class FtpClient
    {
        private NetworkCredential _credential { get; set; }
        private ILogService _log;

        public FtpClient(FtpOptions options, ILogService log)
        {
            _credential = options.GetCredential();
            _log = log;
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
            request.Credentials = _credential;
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
                    _log.Verbose("Upload {ftpPath} status {StatusDescription}", ftpPath, response.StatusDescription?.Trim());
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
                            _log.Verbose("Create {ftpPath} status {StatusDescription}", ftpPath, response.StatusDescription?.Trim());
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Verbose("Create {ftpPath} failed, may already exist ({Message})", ftpPath, ex.Message);
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
            names = names.Trim();
            _log.Verbose("Files in path {ftpPath}: {@names}", ftpPath, names);
            return names;
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
                _log.Verbose("Delete {ftpPath} status {StatusDescription}", ftpPath, response.StatusDescription?.Trim());
            }
        }

        public enum FileType
        {
            File,
            Directory
        }
    }
}