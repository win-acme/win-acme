using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslFactory : BaseStorePluginFactory<CentralSsl>
    {
        public CentralSslFactory(ILogService log) : base(log, nameof(CentralSsl)) { }
    }

    internal class CentralSsl : IStorePlugin
    {
        private ILogService _log;
        private ScheduledRenewal _renewal;

        public CentralSsl(ScheduledRenewal renewal, ILogService log)
        {
            _log = log;
            _renewal = renewal;
            if (!string.IsNullOrWhiteSpace(_renewal.CentralSslStore))
            {
                _log.Debug("Using Centralized SSL path: {CentralSslStore}", _renewal.CentralSslStore);
            }
        }

        public void Save(CertificateInfo input)
        {
            _log.Information("Copying certificate to the Central SSL store");
            var source = input.PfxFile;
            IEnumerable<string> targets = input.HostNames;
            if (source == null)
            {
                source = new FileInfo(Path.Combine(_renewal.CentralSslStore, $"{targets.First()}.pfx"));
                File.WriteAllBytes(source.FullName, input.Certificate.Export(X509ContentType.Pfx));
                targets = targets.Skip(1);
            }
            foreach (var identifier in targets)
            {
                var dest = Path.Combine(_renewal.CentralSslStore, $"{identifier}.pfx");
                _log.Information("Saving certificate to Central SSL location {dest}", dest);
                try
                {
                    File.Copy(input.PfxFile.FullName, dest, !_renewal.KeepExisting ?? false);
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
            var di = new DirectoryInfo(_renewal.CentralSslStore);
            foreach (var fi in di.GetFiles("*.pfx"))
            {
                var cert = LoadCertificate(fi);
                if (cert != null && string.Equals(cert.Thumbprint, input.Certificate.Thumbprint, StringComparison.InvariantCultureIgnoreCase))
                {
                    fi.Delete();
                }
            }
        }

        public CertificateInfo FindByThumbprint(string thumbprint)
        {
            return GetCertificate(CertificateService.ThumbprintFilter(thumbprint));
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
                cert = new X509Certificate2(fi.FullName, Properties.Settings.Default.PFXPassword);
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

        /// <summary>
        /// Find specific certificate in the store
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        private CertificateInfo GetCertificate(Func<X509Certificate2, bool> filter)
        {
            try
            {
                var di = new DirectoryInfo(_renewal.CentralSslStore);
                foreach (var fi in di.GetFiles("*.pfx"))
                {
                    var cert = LoadCertificate(fi);
                    if (cert != null && filter(cert))
                    {
                        return new CertificateInfo() { Certificate = cert, PfxFile = fi };
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
