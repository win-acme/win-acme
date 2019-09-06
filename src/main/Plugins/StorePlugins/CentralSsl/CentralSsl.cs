using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSsl : IStorePlugin
    {
        private ILogService _log;
        private readonly string _path;
        private readonly string _password; 

        public CentralSsl(ILogService log, ISettingsService settings, CentralSslOptions options)
        {
            _log = log;

            if (!string.IsNullOrWhiteSpace(options.PfxPassword?.Value))
            {
                _password = options.PfxPassword.Value;
            }
            else
            {
                _password = settings.DefaultCentralSslPfxPassword;
            }

            if (!string.IsNullOrWhiteSpace(options.Path))
            {
                _path = options.Path;
            }
            else
            {
                _path = settings.DefaultCentralSslStore;
            }
            if (_path.ValidPath(log))
            {
                _log.Debug("Using Centralized SSL path: {_path}", _path);
            }
            else
            {
                throw new Exception($"Specified CentralSsl path {_path} is not valid.");
            }
        }

        public void Save(CertificateInfo input)
        {
            _log.Information("Copying certificate to the Central SSL store");
            var source = input.CacheFile;
            IEnumerable<string> targets = input.HostNames;
            foreach (var identifier in targets)
            {
                var dest = Path.Combine(_path, $"{identifier.Replace("*", "_")}.pfx");
                _log.Information("Saving certificate to Central SSL location {dest}", dest);
                try
                {
                    File.WriteAllBytes(dest, input.Certificate.Export(X509ContentType.Pfx, _password));
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error copying certificate to Central SSL store");
                }
            }
            input.StoreInfo.Add(GetType(),
                new StoreInfo()
                {
                    Name = CentralSslOptions.PluginName,
                    Path = _path
                });
        }

        public void Delete(CertificateInfo input)
        {
            _log.Information("Removing certificate from the Central SSL store");
            var di = new DirectoryInfo(_path);
            foreach (var fi in di.GetFiles("*.pfx"))
            {
                var cert = LoadCertificate(fi);
                if (cert != null && string.Equals(cert.Thumbprint, input.Certificate.Thumbprint, StringComparison.InvariantCultureIgnoreCase))
                {
                    fi.Delete();
                }
            }
        }

        /// <summary>
        /// Load certificate from disk
        /// </summary>
        /// <param name="fi"></param>
        /// <returns></returns>
        private X509Certificate2 LoadCertificate(FileInfo fi)
        {
            X509Certificate2 cert = null;
            try
            {
                cert = new X509Certificate2(fi.FullName, _password);
            }
            catch (CryptographicException)
            {
                try
                {
                    cert = new X509Certificate2(fi.FullName, "");
                }
                catch
                {
                    _log.Warning("Unable to scan certificate {name}", fi.FullName);
                }
            }
            return cert;
        }
    }
}
