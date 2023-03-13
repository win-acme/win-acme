using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading.Tasks;

using static System.IO.FileSystemAclExtensions;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [IPlugin.Plugin<
        CertificateStoreOptions, CertificateStoreOptionsFactory, 
        CertificateStoreCapability, WacsJsonPlugins>
        ("e30adc8e-d756-4e16-a6f2-450f784b1a97", 
        Name, "Windows Certificate Store (Local Computer)")]
    internal class CertificateStore : IStorePlugin, IDisposable
    {
        internal const string Name = "CertificateStore";
        private const string DefaultStoreName = nameof(StoreName.My);
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly string _storeName;
        private readonly IIISClient _iisClient;
        private readonly CertificateStoreOptions _options;
        private readonly FindPrivateKey _keyFinder;
        private readonly CertificateStoreClient _storeClient;
        private readonly RunLevel _runLevel;

        public CertificateStore(
            ILogService log, IIISClient iisClient,
            ISettingsService settings, FindPrivateKey keyFinder, 
            CertificateStoreOptions options, RunLevel runLevel)
        {
            _log = log;
            _iisClient = iisClient;
            _options = options;
            _settings = settings;
            _keyFinder = keyFinder;
            _storeName = options.StoreName ?? DefaultStore(settings, iisClient);
            if (string.Equals(_storeName, "Personal", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(_storeName, "Computer", StringComparison.InvariantCultureIgnoreCase))
            {
                // Users trying to use the "My" store might have set "Personal" in their 
                // config files, because that's what the store is called in mmc
                _storeName = nameof(StoreName.My);
            }
            _storeClient = new CertificateStoreClient(_storeName, StoreLocation.LocalMachine, _log);
            _runLevel = runLevel;
        }

        /// <summary>
        /// Determine the default certificate store
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        public static string DefaultStore(ISettingsService settings, IIISClient client)
        {
            // First priority: specified in settings.json 
            string? storeName;
            try
            {
                storeName = settings.Store.CertificateStore.DefaultStore;
                // Second priority: defaults
                if (string.IsNullOrWhiteSpace(storeName))
                {
                    storeName = client.Version.Major < 8 ? nameof(StoreName.My) : "WebHosting";
                }
            } 
            catch
            {
                storeName = DefaultStoreName;
            }
            return storeName;
        }

        public Task<StoreInfo?> Save(ICertificateInfo input)
        {
            var existing = _storeClient.FindByThumbprint(input.Certificate.Thumbprint);
            var store = input.Certificate;

            if (existing != null)
            {
                _log.Warning("Certificate with thumbprint {thumbprint} is already in the store", input.Certificate.Thumbprint);
                store = existing;
            }
            else
            {
                var exportable = 
                    _settings.Store.CertificateStore.PrivateKeyExportable == true ||
                    #pragma warning disable CS0618 // Type or member is obsolete
                    (_settings.Store.CertificateStore.PrivateKeyExportable == null && _settings.Security.PrivateKeyExportable == true);
                    #pragma warning restore CS0618 // Type or member is obsolete

                var baseFlags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
                var finalFlags = baseFlags;
                if (exportable)
                {
                    finalFlags |= X509KeyStorageFlags.Exportable;
                }
                _log.Debug("Storing certificate with flags {flags}", finalFlags);

                if (_settings.Store.CertificateStore.UseNextGenerationCryptoApi != true)
                {
                    // Should always be exportable before we attempt to convert,
                    // because otherwise we won't be able to get to the private key
                    store = _storeClient.ApplyFlags(store, baseFlags | X509KeyStorageFlags.Exportable);

                    // If the ConvertCertificate fails we fall back to the original
                    // input certificate wit the base flags applied
                    store = _storeClient.ConvertCertificate(store, finalFlags);
                    store ??= _storeClient.ApplyFlags(input.Certificate, baseFlags);
                } 
                else
                {
                    // Do not attempt conversion, just apply the final flags
                    store = _storeClient.ApplyFlags(store, finalFlags);
                }

                _log.Information("Installing certificate in the certificate store");
                _storeClient.InstallCertificate(store);
                if (!_runLevel.HasFlag(RunLevel.Test))
                {
                    _storeClient.InstallCertificateChain(input.Chain);
                }
                store = _storeClient.FindByThumbprint(input.Certificate.Thumbprint);
            }

            if (_options.AclFullControl != null)
            {
                SetAcl(store, _options.AclFullControl);
            }

            return Task.FromResult<StoreInfo?>(new StoreInfo() {
                Name = Name,
                Path = _storeName
            });
        }

        private void SetAcl(X509Certificate2? cert, List<string> fullControl)
        {
            if (cert == null)
            {
                _log.Error("Unable to set requested ACL on private key (certificate not found)");
                return;
            }
            try
            {
                var file = _keyFinder.Find(cert);
                if (file != null)
                {
                    _log.Verbose("Private key found at {dir}", file.FullName);
                    var fs = new FileSecurity(file.FullName, AccessControlSections.All);
                    foreach (var account in fullControl)
                    {
                        try
                        {
                            var principal = new NTAccount(account);
                            fs.AddAccessRule(new FileSystemAccessRule(principal, FileSystemRights.FullControl, AccessControlType.Allow));
                            _log.Information("Add full control rights for {account}", account);
                        }
                        catch (Exception ex)
                        {
                            _log.Warning("Unable to set full control rights for {account}: {ex}", account, ex.Message);
                            _log.Verbose("{ex}", ex.StackTrace);
                        }
                    }
                    file.SetAccessControl(fs);
                } 
                else
                {
                    _log.Error("Unable to set requested ACL on private key (file not found)");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to set requested ACL on private key");
            }
        }

        public Task Delete(ICertificateInfo input)
        {
            // Test if the user manually added the certificate to IIS
            if (_iisClient.HasWebSites)
            {
                var hash = input.Certificate.GetCertHash();
                if (_iisClient.Sites.Any(site =>
                    site.Bindings.Any(binding =>
                    StructuralComparisons.StructuralEqualityComparer.Equals(binding.CertificateHash, hash) &&
                    Equals(binding.CertificateStoreName, _storeName))))
                {
                    _log.Error("The previous certificate was detected in IIS. Configure the IIS installation step to auto-update bindings.");
                    return Task.CompletedTask;
                }
            }
            _storeClient.UninstallCertificate(input.Certificate);
            return Task.CompletedTask;
        }

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _storeClient.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion
    }
}