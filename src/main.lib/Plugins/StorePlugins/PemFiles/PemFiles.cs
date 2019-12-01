using Org.BouncyCastle.Pkcs;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFiles : IStorePlugin
    {
        private readonly ILogService _log;
        private readonly PemService _pemService;

        private readonly string _path;

        public PemFiles(
            ILogService log, ISettingsService settings,
            PemService pemService, PemFilesOptions options)
        {
            _log = log;
            _pemService = pemService;
            _path = !string.IsNullOrWhiteSpace(options.Path) ? 
                options.Path : 
                settings.Store.DefaultPemFilesPath;
            if (_path.ValidPath(log))
            {
                _log.Debug("Using .pem certificate path: {_path}", _path);
            }
            else
            {
                throw new Exception($"Specified PemFiles path {_path} is not valid.");
            }
        }

        public Task Save(CertificateInfo input)
        {
            
            _log.Information("Exporting .pem files to {folder}", _path);
            try
            {
                // Determine name
                var name = input.SubjectName.Replace("*", "_");

                // Base certificate
                var certificateExport = input.Certificate.Export(X509ContentType.Cert);
                var exportString = _pemService.GetPem("CERTIFICATE", certificateExport);
                File.WriteAllText(Path.Combine(_path, $"{name}-crt.pem"), exportString);

                // Rest of the chain
                foreach (var chainCertificate in input.Chain)
                {
                    // Do not include self-signed certificates, root certificates
                    // are supposed to be known already by the client.
                    if (chainCertificate.Subject != chainCertificate.Issuer)
                    {
                        var chainCertificateExport = chainCertificate.Export(X509ContentType.Cert);
                        exportString += _pemService.GetPem("CERTIFICATE", chainCertificateExport);
                    }
                }

                // Save complete chain
                File.WriteAllText(Path.Combine(_path, $"{name}-chain.pem"), exportString);
                input.StoreInfo.Add(GetType(),
                    new StoreInfo()
                    {
                        Name = PemFilesOptions.PluginName,
                        Path = _path
                    });

                // Private key
                var pkPem = "";
                var store = new Pkcs12Store(input.CacheFile.OpenRead(), input.CacheFilePassword.ToCharArray());
                var alias = store.Aliases.OfType<string>().FirstOrDefault(p => store.IsKeyEntry(p));
                if (alias == null)
                {
                    _log.Warning("No key entries found");
                    return Task.CompletedTask;
                }

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
                    _log.Warning("No private key found");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error exporting .pem files to folder");
            }
            return Task.CompletedTask;
        }

        public Task Delete(CertificateInfo input) => Task.CompletedTask;

        public CertificateInfo FindByThumbprint() => null;

        bool IPlugin.Disabled => false;
    }
}
