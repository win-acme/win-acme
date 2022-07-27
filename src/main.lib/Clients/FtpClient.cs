using FluentFTP;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    internal class FtpClient
    {
        private NetworkCredential? Credential { get; set; }
        private readonly ILogService _log;
        private FluentFTP.FtpClient? _cacheClient;

        public FtpClient(
            NetworkCredentialOptions? options,
            ILogService log, 
            SecretServiceManager secretService)
        {
            _log = log;
            if (options != null)
            {
                Credential = options.GetCredential(secretService);
            }
        }

        private async Task<FluentFTP.FtpClient> CreateClient(Uri uri)
        {
            if (_cacheClient == null || 
                !_cacheClient.IsConnected || 
                !_cacheClient.IsAuthenticated)
            {
                var port = uri.Port == -1 ? 21 : uri.Port;
                var client = new FluentFTP.FtpClient(uri.Host, port, Credential?.UserName, Credential?.Password);
                client.ValidateAnyCertificate = true;
                client.OnLogEvent += (level, message) =>
                {
                    switch (level)
                    {
                        case FtpTraceLevel.Verbose:
                            _log.Verbose("FTP: {message}", message);
                            break;
                        case FtpTraceLevel.Info:
                            _log.Information("FTP: {message}", message);
                            break;
                        case FtpTraceLevel.Warn:
                            _log.Warning("FTP: {message}", message);
                            break;
                        case FtpTraceLevel.Error:
                            _log.Error("FTP: {message}", message);
                            break;
                    }
                };
                await client.AutoConnectAsync();
                _cacheClient = client;
                _log.Information("Established connection with ftp server at {host}:{port}, encrypted: {enc}", uri.Host, port, _cacheClient.IsEncrypted);
            }
            return _cacheClient;
        }

        public async Task Upload(string ftpPath, string content)
        {
            var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            var uri = new Uri(ftpPath);
            var client = await CreateClient(uri);
            var status = await client.UploadStreamAsync(stream, uri.PathAndQuery, FtpRemoteExists.Overwrite, true);
            if (status == FtpStatus.Success)
            {
                _log.Debug("Upload {ftpPath} status {StatusDescription}", ftpPath, status);
            }
            else
            {
                _log.Warning("Upload {ftpPath} status {StatusDescription}", ftpPath, status);
            }
        }

        public async Task<IEnumerable<string>> GetFiles(string ftpPath)
        {
            var uri = new Uri(ftpPath);
            var client = await CreateClient(uri);
            var list = await client.GetListingAsync(uri.PathAndQuery);
            if (list == null)
            {
                return new List<string>();
            }
            return list.Select(x => x.Name);
        }

        public async Task DeleteFolder(string ftpPath)
        {
            var uri = new Uri(ftpPath);
            var client = await CreateClient(uri);
            await client.DeleteDirectoryAsync(uri.PathAndQuery);
            _log.Debug("Delete folder {ftpPath}", ftpPath);
        }

        public async Task DeleteFile(string ftpPath)
        {
            var uri = new Uri(ftpPath);
            var client = await CreateClient(uri);
            await client.DeleteFileAsync(uri.PathAndQuery);
            _log.Debug("Delete file {ftpPath}", ftpPath);
        }
    }
}