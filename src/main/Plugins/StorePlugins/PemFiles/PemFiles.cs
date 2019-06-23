using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Properties;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFiles : IStorePlugin
    {
        private ILogService _log;
        private PemService _pemService;

        private readonly string _path;

        public PemFiles(ILogService log, PemService pemService, PemFilesOptions options)
        {
            _log = log;
            _pemService = pemService;
            if (!string.IsNullOrWhiteSpace(options.Path))
            {
                _path = options.Path;
            }
            else
            {
                _path = Settings.Default.DefaultPemFilesPath;
            }
            if (_path.ValidPath(log))
            {
                _log.Debug("Using .pem certificate path: {_path}", _path);
            }
            else
            {
                throw new Exception("Error initializing PemFiles plugin, specified path is not valid.");
            }
        }

        public void Save(CertificateInfo input)
        {
            _log.Information("Exporting .pem files to {folder}", _path);
            try
            {
                // Base certificate
                var certificateExport = input.Certificate.Export(X509ContentType.Cert);
                var exportString = _pemService.GetPem("CERTIFICATE", certificateExport);

                // Rest of the chain
                var chain = new X509Chain();
                chain.Build(input.Certificate);
                for (var i = 1; i < chain.ChainElements.Count; i++)
                {
                    var chainCertificate = chain.ChainElements[i].Certificate;
                    // Do not include self-signed certificates, root certificates
                    // are supposed to be known already by the client.
                    if (chainCertificate.Subject != chainCertificate.Issuer)
                    {
                        var chainCertificateExport = chainCertificate.Export(X509ContentType.Cert);
                        exportString += _pemService.GetPem("CERTIFICATE", chainCertificateExport);
                    }

                }
  
                // Determine name
                var name = input.SubjectName.Replace("*", "_");

                // Save complete chain
                File.WriteAllText(Path.Combine(_path, $"{name}-chain.pem"), exportString);

                // Private key
                var pkPem = "";
                var store = new Pkcs12Store(input.CacheFile.OpenRead(), input.CacheFilePassword.ToCharArray());
                var alias = store.Aliases.OfType<string>().FirstOrDefault(p => store.IsKeyEntry(p));
                var entry = store.GetKey(alias);
                var key = entry.Key;
                if (key.IsPrivate)
                {
                    pkPem = _pemService.GetPem(entry.Key);
                }
                if (!string.IsNullOrEmpty(pkPem))
                {
                    File.WriteAllText(Path.Combine(_path, $"{name}-key.pem"), pkPem);
                }
                else
                {
                    _log.Error("Unable to read private key");
                }

                input.StoreInfo.Add(GetType(),
                    new StoreInfo()
                    {
                        Name = PemFilesOptions.PluginName,
                        Path = _path
                    });
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error exporting .pem files to folder");
            }

        }

        public void Delete(CertificateInfo input)
        {
            // Not supported
        }

        public CertificateInfo FindByThumbprint(string thumbprint)
        {
            return null;
        }
    }
}
