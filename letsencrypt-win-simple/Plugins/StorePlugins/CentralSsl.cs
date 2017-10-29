using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Services
{
    class CentralSsl : IStorePlugin
    {
        public string Name => nameof(CentralSsl);
        public string Description => nameof(CentralSsl);

        private ILogService _log;
        private Options _options;
        private CertificateService _certificateService;

        public CentralSsl(Options options, ILogService log, CertificateService certificateService)
        {
            _log = log;
            _options = options;
            _certificateService = certificateService;
            if (_options.CentralSsl)
            {
                _log.Debug("Using Centralized SSL path: {CentralSslStore}", _options.CentralSslStore);
            }
        }

        public void Save(CertificateInfo input)
        {
            _log.Information("Copying certificate to the Central SSL store");
            if (input.PfxFile == null || input.PfxFile.Exists == false)
            {
                // PFX doesn't exist yet, let's create one
                input.PfxFile = new FileInfo(_certificateService.PfxFilePath(input.HostNames.First()));
                File.WriteAllBytes(input.PfxFile.FullName, input.Certificate.Export(X509ContentType.Pfx));
            }
            foreach (var identifier in input.HostNames)
            {
                var dest = Path.Combine(_options.CentralSslStore, $"{identifier}.pfx");
                _log.Information("Saving certificate to Central SSL location {dest}", dest);
                try
                {
                    File.Copy(input.PfxFile.FullName, dest, !_options.KeepExisting);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error copying certificate to Central SSL store");
                }
            }
        }

        public void Delete(CertificateInfo input)
        {
            _log.Information("Removing certificate from the Central SSL store");
            var di = new DirectoryInfo(_options.CentralSslStore);
            foreach (var fi in di.GetFiles("*.pfx"))
            {
                var cert = new X509Certificate2(fi.FullName, Properties.Settings.Default.PFXPassword);
                if (string.Equals(cert.Thumbprint, input.Certificate.Thumbprint, StringComparison.InvariantCultureIgnoreCase))
                {
                    fi.Delete();
                }
            }
        }

        public CertificateInfo FindByFriendlyName(string friendlyName)
        {
            return GetCertificate(CertificateService.FriendlyNameFilter(friendlyName));
        }

        public CertificateInfo FindByThumbprint(string thumbprint)
        {
            return GetCertificate(CertificateService.ThumbprintFilter(thumbprint));
        }

        /// <summary>
        /// Find specific certificate in the store
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        private CertificateInfo GetCertificate(Func<X509Certificate2, bool> filter)
        {
            X509Certificate2 ret = null;
            FileInfo pfx = null;
            try
            {
                var di = new DirectoryInfo(_options.CentralSslStore);
                foreach (var fi in di.GetFiles("*.pfx"))
                {
                    var cert = new X509Certificate2(fi.FullName, Properties.Settings.Default.PFXPassword);
                    if (filter(cert))
                    {
                        return new CertificateInfo() { Certificate = ret, PfxFile = pfx };
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error finding certificate in Central SSL store");
                throw;
            }
            return null;
        }
    }
}
