using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    public class CertificateStoreClient : IDisposable
    {
        private readonly X509Store _store;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly StoreLocation _location;
        private bool disposedValue;

        public CertificateStoreClient(string storeName, StoreLocation storeLocation, ILogService log, ISettingsService settings)
        {
            _log = log;
            _location = storeLocation;
            _store = new X509Store(storeName, storeLocation);
            _settings = settings;
        }

        public X509Certificate2? FindByThumbprint(string thumbprint) => GetCertificate(x => string.Equals(x.Thumbprint, thumbprint));

        /// <summary>
        /// Install the main certs with potential private key
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public bool InstallCertificate(ICertificateInfo certificate, X509KeyStorageFlags flags)
        {
            // Determine storage flags
            var exportable =
                _settings.Store.CertificateStore.PrivateKeyExportable == true ||
                #pragma warning disable CS0618 // Type or member is obsolete
                (_settings.Store.CertificateStore.PrivateKeyExportable == null && _settings.Security.PrivateKeyExportable == true);
                #pragma warning restore CS0618 // Type or member is obsolete
            if (exportable)
            {
                flags |= X509KeyStorageFlags.Exportable;
            }
            flags |= X509KeyStorageFlags.PersistKeySet;

            var dotnet = default(X509Certificate2); 
            if (_settings.Store.CertificateStore.UseNextGenerationCryptoApi != true)
            {
                // We should always be exportable before we can try 
                // conversion to the legacy Crypto API. Look for the
                // certificate with the private key attached.

                // Adding password protection to these temporary certificates 
                // might cause difficult to reproduce bugs during later
                // stages of the process, so we've removed them for now.
                dotnet = certificate.AsCollection(X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable).OfType<X509Certificate2>().FirstOrDefault(x => x.HasPrivateKey);
                if (dotnet != null)
                {
                    dotnet = ConvertCertificate(dotnet, flags);
                }
            }
            if (dotnet == null)
            {
                // If conversion failed or was not attempted, use original set of flags
                // but here we should consider the scenario that the private key is not 
                // present at all.
                var collection = certificate.AsCollection(flags).OfType<X509Certificate2>().ToList();
                dotnet = collection.FirstOrDefault(x => !collection.Any(y => x.Subject == y.Issuer));
            }
            if (dotnet != null)
            {
                SaveToStore(_store, dotnet, true);
            }
            return exportable;
        }

        /// <summary>
        /// Actual save to store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="dotnet"></param>
        private void SaveToStore(X509Store store, X509Certificate2 dotnet, bool overwrite)
        {
            var close = false;
            if (!store.IsOpen)
            {
                _log.Debug("Open store {name}", store.Name);
                store.Open(OpenFlags.ReadWrite);
                close = true;
            }
            var found = store.Certificates.Find(X509FindType.FindByThumbprint, dotnet.Thumbprint, false).Count > 1;
            if (!found || overwrite)
            {
                try
                {
                    _log.Information(LogType.All, "{verb} certificate {FriendlyName} in store {name}", found ? "Replacing" : "Adding", dotnet.FriendlyName ?? dotnet.Subject, store.Name);
                    _log.Verbose("{sub}/{iss} ({thumb})", dotnet.Subject, dotnet.Issuer, dotnet.Thumbprint);
                    store.Add(dotnet);
                }
                catch
                {
                    _log.Error("Error saving certificate");
                    throw;
                }
            } 
            else
            {
                _log.Verbose("{sub} - {iss} ({thumb}) already exists in {store}", dotnet.Subject, dotnet.Issuer, dotnet.Thumbprint, store.Name);
            }
            if (close)
            {
                _log.Debug("Close store {name}", store.Name);
                store.Close();
            }
        }

        /// <summary>
        /// Install the chain certs
        /// </summary>
        /// <param name="certificate"></param>
        public void InstallCertificateChain(ICertificateInfo certificate)
        {
            using var imStore = new X509Store(StoreName.CertificateAuthority, _location);
            var store = imStore;
            try
            {
                imStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                if (!imStore.IsOpen)
                {
                    throw new Exception("Store failed to open");
                }
            }
            catch (Exception ex)
            {
                _log.Warning($"Error opening intermediate certificate store: {ex.Message}");
                store = _store;
            }
            foreach (var bcCert in certificate.Chain)
            {
                var cert = new X509Certificate2(bcCert.GetEncoded());
                SaveToStore(store, cert, false);
            }
            store.Close();
        }

        /// <summary>
        /// Remove superfluous certificate from the store
        /// </summary>
        /// <param name="thumbprint"></param>
        public void UninstallCertificate(string thumbprint)
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

        /// <summary>
        /// Set the right flags on the certificate and
        /// convert the private key to the right cryptographic
        /// provider
        /// </summary>
        /// <param name="original"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private X509Certificate2? ConvertCertificate(X509Certificate2 original, X509KeyStorageFlags flags, string? password = null)
        {
            try
            {
                // If there is an RSA key, we change it to be stored in the
                // Microsoft RSA SChannel Cryptographic Provider so that its 
                // usable for older versions of Microsoft Exchange and exportable
                // from IIS. This also is required to allow the SetAcl logic to 
                // work.
                using var rsaPrivateKey = original.GetRSAPrivateKey();
                if (rsaPrivateKey == null)
                {
                    return null;
                }
                else
                {
                    _log.Debug("Converting private key...");
                }

                // Export private key parameters
                // https://github.com/dotnet/runtime/issues/36899
                using var tempRsa = RSA.Create();
                var pbeParameters = new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithmName.SHA256, 10);
                tempRsa.ImportEncryptedPkcs8PrivateKey(password, rsaPrivateKey.ExportEncryptedPkcs8PrivateKey(password, pbeParameters), out var read);

                var cspFlags = CspProviderFlags.NoPrompt;
                if (flags.HasFlag(X509KeyStorageFlags.MachineKeySet))
                {
                    cspFlags |= CspProviderFlags.UseMachineKeyStore;
                }
                if (!flags.HasFlag(X509KeyStorageFlags.Exportable))
                {
                    cspFlags |= CspProviderFlags.UseNonExportableKey;
                }
                var cspParameters = new CspParameters
                {
                    KeyContainerName = Guid.NewGuid().ToString(),
                    KeyNumber = 1,
                    Flags = cspFlags,
                    ProviderType = 12 // Microsoft RSA SChannel Cryptographic Provider
                };
                var rsaProvider = new RSACryptoServiceProvider(cspParameters);
                var parameters = tempRsa.ExportParameters(true);
                rsaProvider.ImportParameters(parameters);

                var tempPfx = new X509Certificate2(original.Export(X509ContentType.Cert, password), password, flags);
                tempPfx = tempPfx.CopyWithPrivateKey(rsaProvider);
                tempPfx.FriendlyName = original.FriendlyName;
                return tempPfx;
            }
            catch (Exception ex)
            {
                // If we couldn't convert the private key that 
                // means we're left with a pfx generated with the
                // 'wrong' Crypto provider
                _log.Warning("Error converting key to legacy CryptoAPI, using CNG instead.");
                _log.Verbose("{ex}", ex);
                return null;
            }
        }

        /// <summary>
        /// Find certificate in the store
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
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
