using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
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
        private CertificateService _certificateService;

        private readonly string _path;

        public PemFiles(ILogService log, CertificateService certificateService, PemFilesOptions options)
        {
            _log = log;
            _certificateService = certificateService;
            if (!string.IsNullOrWhiteSpace(options.Path))
            {
                _path = options.Path;
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
                var crtPem = _certificateService.GetPem("CERTIFICATE", certificateExport);

                // Issuer certificate
                var chain = new X509Chain();
                chain.Build(input.Certificate);
                X509Certificate2 issuerCertificate = chain.ChainElements[1].Certificate;
                var issuerCertificateExport = issuerCertificate.Export(X509ContentType.Cert);
                var issuerPem = _certificateService.GetPem("CERTIFICATE", issuerCertificateExport);

                // Save complete chain
                File.WriteAllText(Path.Combine(_path, $"{input.SubjectName}-chain.pem"), crtPem + issuerPem);

                // Private key
                var pkPem = "";
                var store = new Pkcs12Store(input.PfxFile.OpenRead(), input.PfxFilePassword.ToCharArray());
                var alias = store.Aliases.OfType<string>().FirstOrDefault(p => store.IsKeyEntry(p));
                var entry = store.GetKey(alias);
                var key = entry.Key;
                if (key.IsPrivate)
                {
                    pkPem = _certificateService.GetPem(entry.Key);
                }
                if (!string.IsNullOrEmpty(pkPem))
                {
                    File.WriteAllText(Path.Combine(_path, $"{input.SubjectName}-key.pem"), pkPem);
                }
                else
                {
                    _log.Error("Unable to read private key");
                }
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
