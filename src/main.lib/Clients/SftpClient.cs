using Autofac;
using PKISharp.WACS.Services;
using Renci.SshNet;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace PKISharp.WACS.Clients
{
    internal class SshFtpClient
    {
        private readonly NetworkCredential? _credential;
        private readonly ILogService _log;

        /// <summary>
        /// Creating an Instance of SSH FTP Client.
        /// </summary>
        /// <param name="credential">Pass the credentials used for SSH (no certificate authentication).</param>
        /// <param name="log">Logging service has to be passed.</param>
        public SshFtpClient(NetworkCredential? credential, ILogService log)
        {
            _credential = credential;
            _log = log;
        }

        private Uri CreateUri(string path)
        {
            var sftpUriBuilder = new UriBuilder(new Uri(path));
            if (sftpUriBuilder.Port == -1)
            {
                sftpUriBuilder.Port = 22;
            }
            return sftpUriBuilder.Uri;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sftpPathWithHost"></param>
        /// <returns></returns>
        private SftpClient CreateRequest(Uri uri)
        {
            if (_credential == null)
            {
                throw new InvalidOperationException("No credentials have been provided");
            }
            // Create connection information
            // TODO Add Certificate authentication later on
            var connectionInfo = new ConnectionInfo(
                uri.Host, 
                uri.Port,
                _credential.UserName,
                new PasswordAuthenticationMethod(_credential.UserName, _credential.Password));

            // Create client with specified authentication
            var client = new SftpClient(connectionInfo);

            // Return new client
            return client;
        }

        /// <summary>
        /// Upload file to sftp
        /// </summary>
        /// <param name="sftpPathWithHost">full path as string</param>
        /// <param name="content">content as string</param>
        public void Upload(string sftpPathWithHost, string content)
        {
            // Directory has to exist
            var uri = CreateUri(sftpPathWithHost);
            EnsureDirectories(uri);

            // Start upload process
            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            // Write content into memorystream
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            // Setup connection
            var client = CreateRequest(uri);
            client.Connect();

            // Copy data onto sftp
            client.UploadFile(stream, uri.AbsolutePath);

            // Log for debugging
            var statusDescription = client.Exists(uri.AbsolutePath) ? "Completed" : "Failed";
            _log.Verbose("Upload {sftpPath} status {StatusDescription}", sftpPathWithHost, statusDescription);

            // Close connection
            client.Disconnect();
            client.Dispose();
        }

        /// <summary>
        /// Ensures that the directory exists, if not, it will be created
        /// </summary>
        /// <param name="sftpPathWithHost">full path as string</param>
        private void EnsureDirectories(Uri uri)
        {
            // Setup connection
            var client = CreateRequest(uri);
            client.Connect();

            // Get Directories
            var directories = uri.AbsolutePath.Split('/');

            // Check existance
            client.ChangeDirectory("/");

            if (directories.Length > 1)
            {
                // Start at one, because the first entry will be empty - Copied codestyle from FtpClient
                for (var i = 1; i < (directories.Length - 1); i++)
                {
                    if (client.Exists(directories[i]))
                    {
                        // Log for debugging
                        _log.Verbose("Create {sftpPath} failed, may already exist", uri);
                    }
                    else
                    {
                        // Create directory, to ensure it exists
                        client.CreateDirectory(directories[i]);

                        // Log for debugging
                        var statusDescription = client.Exists(directories[i]) ? "Exists" : "Does not exist";
                        _log.Verbose("Create {sftpPath} status {StatusDescription}", uri, statusDescription);
                    }

                    client.ChangeDirectory(directories[i]);
                }
            }

            // Close connection
            client.Disconnect();
            client.Dispose();
        }

        /// <summary>
        /// Get a list of files from a path.
        /// </summary>
        /// <param name="sftpPathWithHost">SFTP Path</param>
        /// <returns>Strings seperated by comma (e.g. "File.txt, File2.txt").</returns>
        public string GetFiles(string sftpPathWithHost)
        {
            // Setup connection
            var uri = CreateUri(sftpPathWithHost);
            var client = CreateRequest(uri);
            client.Connect();

            // Get file list
            client.ChangeDirectory(uri.AbsolutePath);
            var fileList = client.ListDirectory(client.WorkingDirectory).Where(it => it.IsRegularFile).Select(it => it.FullName);

            // Close connection
            client.Disconnect();
            client.Dispose();

            // Create appropriate return value
            var returnValue = string.Join(", ", fileList).Trim();

            // Log for debugging
            _log.Verbose("Files in path {sftpPath}: {@returnValue}", sftpPathWithHost, returnValue);

            // Return necessary values
            return returnValue;
        }

        /// <summary>
        /// Deletes a single file or an entire directory
        /// </summary>
        /// <param name="sftpPath">full path as string</param>
        /// <param name="fileType">File or Directory?</param>
        public void Delete(string sftpPathWithHost, FileType fileType)
        {
            // Setup connection
            var uri = CreateUri(sftpPathWithHost);
            var client = CreateRequest(uri);
            client.Connect();

            // Check for file or directory and delete
            client.ChangeDirectory("/");

            if (client.Exists(uri.AbsolutePath))
            {
                if (fileType == FileType.Directory)
                {
                    client.DeleteDirectory(uri.AbsolutePath);
                }
                else if (fileType == FileType.File)
                {
                    client.DeleteFile(uri.AbsolutePath);
                }
                else
                {
                    throw new NotImplementedException();
                }

                // Log for debugging
                var statusDescription = client.Exists(uri.AbsolutePath) ? "Not deleted" : "Deleted";
                _log.Verbose("Delete {sftpPath} status {StatusDescription}", sftpPathWithHost, statusDescription);
            }

            // Close connection
            client.Disconnect();
            client.Dispose();
        }

        public enum FileType
        {
            File,
            Directory
        }
    }
}