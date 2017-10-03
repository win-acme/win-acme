using System;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Services
{
    class CertificateStoreService
    {
        private LogService _log;
        private string _certificateStore = "WebHosting";
        private string _configPath;
        private Options _options;
        private X509Store _store;

        public CertificateStoreService(Options options, LogService log)
        {
            _log = log;
            _options = options;
            ParseCertificateStore();
        }

        private void ParseCertificateStore()
        {
            try
            {
                _certificateStore = Properties.Settings.Default.CertificateStore;
                _log.Debug("Certificate store: {_certificateStore}", _certificateStore);
            }
            catch (Exception ex)
            {
                _log.Warning("Error reading CertificateStore from config, defaulting to {_certificateStore} Error: {@ex}", _certificateStore, ex);
            }
        }

        public X509Store DefaultStore
        {
            get {
                if (_store == null)
                {
                    _store = new X509Store(_certificateStore, StoreLocation.LocalMachine);
                }
                return _store;
            }
        }

        public void InstallCertificate(X509Certificate2 certificate, X509Store store = null)
        {
            X509Store imStore = null;
            store = store ?? DefaultStore;
            try
            {
                imStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
                //rootStore = new X509Store(StoreName.AuthRoot, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                imStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                //rootStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                _log.Error("Error encountered while opening certificate store. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }
            _log.Debug("Opened Certificate Store {Name}", store.Name);

            try
            {
                _log.Debug("Adding certificate {FriendlyName} to store", certificate.FriendlyName);
                X509Chain chain = new X509Chain();
                chain.Build(certificate);
                foreach (var chainElement in chain.ChainElements)
                {
                    var cert = chainElement.Certificate;
                    if (cert.HasPrivateKey)
                    {
                        store.Add(cert);
                    }
                    else
                    {
                        imStore.Add(cert);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error saving certificate {@ex}", ex);
            }
            _log.Debug("Closing certificate store");
            store.Close();
            imStore.Close();
            //rootStore.Close();
        }

        public void UninstallCertificate(string thumbprint, X509Store store = null)
        {
            store = store ?? DefaultStore;
            try
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store");
                throw;
            }

            _log.Debug("Opened certificate store {Name}", store.Name);
            try
            {
                X509Certificate2Collection col = store.Certificates;
                foreach (var cert in col)
                {
                    if (string.Equals(cert.Thumbprint, thumbprint, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _log.Information("Removing certificate {cert}", cert.FriendlyName);
                        store.Remove(cert);
                    }
                }
                _log.Debug("Closing certificate store");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error removing certificate");
                throw;
            }
            store.Close();
        }

        /// <summary>
        /// Legecy way to find a certificate, by looking for the friendly name.
        /// This should be removed for a v2.0.0 release
        /// </summary>
        /// <param name="friendlyName"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        public X509Certificate2 GetCertificateByFriendlyName(string friendlyName, X509Store store = null)
        {
            return GetCertificate(CertificateService.FriendlyNameFilter(friendlyName), store);
        }

        /// <summary>
        /// Best way to uniquely find a certificate, be comparing thumbprints
        /// </summary>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        /// <returns></returns>
        public X509Certificate2 GetCertificateByThumbprint(string thumbprint, X509Store store = null)
        {
            return GetCertificate(CertificateService.ThumbprintFilter(thumbprint), store);
        }

        private X509Certificate2 GetCertificate(Func<X509Certificate2, bool> filter, X509Store store = null)
        {
            store = store ?? DefaultStore;
            X509Certificate2 ret = null;
            try
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store");
                throw;
            }
            try
            {
                X509Certificate2Collection col = store.Certificates;
                foreach (var cert in col)
                {
                    if (filter(cert))
                    {
                        ret = cert;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error finding certificate in Certificate Store");
                throw;
            }
            store.Close();
            return ret;
        }
    }
}
