using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    public class CertificateStoreClient : IDisposable
    {
        private readonly X509Store _store;
        private X509Store? _imStore;
        private readonly ILogService _log;
        private readonly StoreLocation _location;
        private bool disposedValue;

        public CertificateStoreClient(string storeName, StoreLocation storeLocation, ILogService log)
        {
            _log = log;
            _location = storeLocation;
            _log.Debug("Certificate store name: {_storeName}", storeName);
            _store = new X509Store(storeName, storeLocation);
        }

        public X509Certificate2? FindByThumbprint(string thumbprint) => GetCertificate(x => string.Equals(x.Thumbprint, thumbprint));

        public void InstallCertificate(X509Certificate2 certificate)
        {
            try
            {
                _store.Open(OpenFlags.ReadWrite);
                _log.Debug("Opened certificate store {Name}", _store.Name);
            }
            catch
            {
                _log.Error("Error encountered while opening certificate store {name}", _store.Name);
                throw;
            }

            try
            {
                _log.Information(LogType.All, "Adding certificate {FriendlyName} to store {name}", certificate.FriendlyName, _store.Name);
                _log.Verbose("{sub} - {iss} ({thumb})", certificate.Subject, certificate.Issuer, certificate.Thumbprint);
                _store.Add(certificate);
            }
            catch
            {
                _log.Error("Error saving certificate");
                throw;
            }
            _log.Debug("Closing certificate store");
            _store.Close();
        }

        public void InstallCertificateChain(IEnumerable<X509Certificate2> chain)
        {
            X509Store? imStore;
            try
            {
                _imStore = new X509Store(StoreName.CertificateAuthority, _location);
                _imStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                if (!_imStore.IsOpen)
                {
                    throw new Exception("not opened");
                }
                imStore = _imStore;
            }
            catch (Exception ex)
            {
                _log.Warning($"Error opening intermediate certificate store: {ex.Message}");
                return;
            }
            imStore ??= _store;
            foreach (var cert in chain)
            {
                if (imStore.Certificates.Find(X509FindType.FindByThumbprint, cert.Thumbprint, false) == null)
                {
                    try
                    {
                        _log.Verbose("{sub} - {iss} ({thumb}) to store {store}", cert.Subject, cert.Issuer, cert.Thumbprint, imStore.Name);
                        imStore.Add(cert);
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("Error saving certificate to store {store}: {message}", imStore.Name, ex.Message);
                    }
                }
                else
                {
                    _log.Verbose("{sub} - {iss} ({thumb}) already exists in {store}", cert.Subject, cert.Issuer, cert.Thumbprint, imStore.Name);
                }
            }

            _log.Debug("Closing store {store}", imStore.Name);
            imStore.Close();
        }

        public void UninstallCertificate(X509Certificate2 certificate)
        {
            _log.Information("Uninstalling certificate from the certificate store");
            try
            {
                _store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store");
                throw;
            }
            _log.Debug("Opened certificate store {Name}", _store.Name);
            try
            {
                var col = _store.Certificates;
                var thumbprint = certificate.Thumbprint;
                foreach (var cert in col)
                {
                    if (string.Equals(cert.Thumbprint, thumbprint, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _log.Information(LogType.All, "Removing certificate {cert} from store {name}", cert.FriendlyName, _store.Name);
                        _store.Remove(cert);
                    }
                }
                _log.Debug("Closing certificate store");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error removing certificate");
                throw;
            }
            _store.Close();
        }

        public X509Certificate2? GetCertificate(Func<X509Certificate2, bool> filter)
        {
            var possibles = new List<X509Certificate2>();
            try
            {
                _store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error encountered while opening certificate store");
                return null;
            }
            try
            {
                var col = _store.Certificates;
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
            _store.Close();
            return possibles.OrderByDescending(x => x.NotBefore).FirstOrDefault();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _store.Dispose();
                    _imStore?.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
