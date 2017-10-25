using LetsEncrypt.ACME.Simple.Clients;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Services
{
    class CertificateStoreService
    {
        private ILogService _log;
        private const string _defaultStore = nameof(StoreName.My);
        private string _certificateStore = _defaultStore;
        private Options _options;
        private X509Store _store;

        public CertificateStoreService(Options options, ILogService log)
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
                if (string.IsNullOrEmpty(_certificateStore))
                {
                    // Default store should be WebHosting on IIS8+, and My (Personal) for IIS7.x
                    if (IISClient.Version.Major < 8)
                    {
                        _certificateStore = nameof(StoreName.My);
                    }
                    else
                    {
                        _certificateStore = "WebHosting";
                    }
                }
                else if (string.Equals(_certificateStore, "Personal", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Users trying to use the "My" store might have set "Personal" in their 
                    // config files, because that's what the store is called in mmc
                    _certificateStore = nameof(StoreName.My);
                }
                _log.Debug("Certificate store: {_certificateStore}", _certificateStore);
            }
            catch (Exception ex)
            {
                _log.Warning("Error reading CertificateStore from config, defaulting to {_certificateStore} Error: {@ex}", _defaultStore, ex);
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
            X509Store rootStore = null;
            try
            {
                rootStore = new X509Store(StoreName.AuthRoot, StoreLocation.LocalMachine);
                rootStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch
            {
                _log.Warning("Error encountered while opening root store");
                rootStore = null;
            }

            X509Store imStore = null;
            try
            {
                imStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
                imStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch
            {
                _log.Warning("Error encountered while opening intermediate certificate store");
                imStore = null;
            }

            try
            {
                store = store ?? DefaultStore;
                store.Open(OpenFlags.ReadWrite);
                _log.Debug("Opened certificate store {Name}", store.Name);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store {name}", store.Name);
                throw;
            }

            try
            {
                _log.Information(true, "Adding certificate {FriendlyName} to store {name}", certificate.FriendlyName, store.Name);
                X509Chain chain = new X509Chain();
                chain.Build(certificate);
                foreach (var chainElement in chain.ChainElements)
                {
                    var cert = chainElement.Certificate;
                    if (cert.HasPrivateKey)
                    {
                        _log.Verbose("{sub} - {iss} ({thumb})", cert.Subject, cert.Issuer, cert.Thumbprint);
                        store.Add(cert);
                    }
                    else if (cert.Subject != cert.Issuer && imStore != null)
                    {
                        _log.Verbose("{sub} - {iss} ({thumb}) to CA store", cert.Subject, cert.Issuer, cert.Thumbprint);
                        imStore.Add(cert);
                    }
                    else if (cert.Subject == cert.Issuer && rootStore != null)
                    {
                        _log.Verbose("{sub} - {iss} ({thumb}) to AuthRoot", cert.Subject, cert.Issuer, cert.Thumbprint);
                        rootStore.Add(cert);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error saving certificate");
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
                        _log.Information(true, "Removing certificate {cert} from store {name}", cert.FriendlyName, store.Name);
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
            var possibles = new List<X509Certificate2>();
            try
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store");
                return null;
            }
            try
            {
                X509Certificate2Collection col = store.Certificates;
                foreach (var cert in col)
                {
                    if (filter(cert))
                    {
                        possibles.Add(cert);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error finding certificate in certificate store");
                return null;
            }
            store.Close();
            return possibles.OrderByDescending(x => x.NotBefore).FirstOrDefault();
        }
    }
}
