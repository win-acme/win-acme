using ACMESharp;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using ACMESharp.PKI.RSA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using LetsEncrypt.ACME.Simple.Extensions;

namespace LetsEncrypt.ACME.Simple.Services
{
    class CentralSslService
    {
        private ILogService _log;
        private Options _options;
        private CertificateService _certificateService;

        public CentralSslService(Options options, ILogService log, CertificateService certificateService)
        {
            _log = log;
            _options = options;
            _certificateService = certificateService;
        }

        /// <summary>
        /// Copy certificata to the Central SSL store
        /// </summary>
        /// <param name="bindings"></param>
        /// <param name="certificate"></param>
        /// <param name="certificatePfx"></param>
        public void InstallCertificate(List<string> bindings, X509Certificate2 certificate, FileInfo certificatePfx)
        {
            if (certificatePfx == null || certificatePfx.Exists == false)
            {
                // PFX doesn't exist yet, let's create one
                certificatePfx = new FileInfo(_certificateService.PfxFilePath(bindings.First()));
                File.WriteAllBytes(certificatePfx.FullName, certificate.Export(X509ContentType.Pfx));
            }

            foreach (var identifier in bindings)
            {
                var dest = Path.Combine(_options.CentralSslStore, $"{identifier}.pfx");
                _log.Information("Saving certificate to Central SSL location {dest}", dest);
                try
                {
                    File.Copy(certificatePfx.FullName, dest, !_options.KeepExisting);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error copying certificate to Central SSL store");
                }
            }
        }


        /// <summary>
        /// Delete certificate from the Central SSL store
        /// </summary>
        /// <param name="thumbprint"></param>
        public void UninstallCertificate(string thumbprint)
        {
            var di = new DirectoryInfo(_options.CentralSslStore);
            foreach (var fi in di.GetFiles("*.pfx"))
            {
                X509Certificate2 cert = LoadCertificate(fi);
                if (cert != null && string.Equals(cert.Thumbprint, thumbprint, StringComparison.InvariantCultureIgnoreCase))
                {
                    fi.Delete();
                }
            }
        }

        /// <summary>
        /// Legecy way to find a certificate, by looking for the friendly name.
        /// This should be removed for a v2.0.0 release
        /// </summary>
        /// <param name="friendlyName"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        public X509Certificate2 GetCertificateByFriendlyName(string friendlyName)
        {
            return GetCertificate(CertificateService.FriendlyNameFilter(friendlyName));
        }

        /// <summary>
        /// Best way to uniquely find a certificate, be comparing thumbprints
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        public X509Certificate2 GetCertificateByThumbprint(string thumbprint)
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
        private X509Certificate2 GetCertificate(Func<X509Certificate2, bool> filter)
        {
            X509Certificate2 ret = null;
            try
            {
                var di = new DirectoryInfo(_options.CentralSslStore);
                foreach (var fi in di.GetFiles("*.pfx"))
                {
                    X509Certificate2 cert = LoadCertificate(fi);
                    if (cert != null && filter(cert))
                    {
                        return cert;
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error finding certificate in Central SSL store");
                throw;
            }
        }
    }
}
