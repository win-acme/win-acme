using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using static System.IO.FileSystemAclExtensions;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Collections;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStore : IStorePlugin, IDisposable
    {
        private const string DefaultStoreName = nameof(StoreName.My);
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly string _storeName;
        private readonly X509Store _store;
        private readonly IIISClient _iisClient;
        private readonly CertificateStoreOptions _options;
        private readonly IUserRoleService _userRoleService;
        private readonly FindPrivateKey _keyFinder;

        public CertificateStore(
            ILogService log, IIISClient iisClient,
            ISettingsService settings, IUserRoleService userRoleService,
            FindPrivateKey keyFinder, CertificateStoreOptions options)
        {
            _log = log;
            _iisClient = iisClient;
            _options = options;
            _settings = settings;
            _userRoleService = userRoleService;
            _keyFinder = keyFinder;
            _storeName = options.StoreName ?? DefaultStore(settings, iisClient);
            if (string.Equals(_storeName, "Personal", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(_storeName, "Computer", StringComparison.InvariantCultureIgnoreCase))
            {
                // Users trying to use the "My" store might have set "Personal" in their 
                // config files, because that's what the store is called in mmc
                _storeName = nameof(StoreName.My);
            }
            _log.Debug("Certificate store: {_certificateStore}", _storeName);
            _store = new X509Store(_storeName!, StoreLocation.LocalMachine);
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
                storeName = settings.Store.CertificateStore?.DefaultStore;
                if (string.IsNullOrWhiteSpace(storeName))
                {
                    storeName = settings.Store.DefaultCertificateStore;
                }
                // Second priority: defaults
                if (string.IsNullOrWhiteSpace(storeName))
                {
                    // Default store should be WebHosting on IIS8+, and My (Personal) for IIS7.x
                    storeName = client.Version.Major < 8 ? nameof(StoreName.My) : "WebHosting";
                }
            } 
            catch
            {
                storeName = DefaultStoreName;
            }
            return storeName;
        }

        public Task Save(CertificateInfo input)
        {
            var existing = FindByThumbprint(input.Certificate.Thumbprint);
            if (existing != null)
            {
                _log.Warning("Certificate with thumbprint {thumbprint} is already in the store", input.Certificate.Thumbprint);
            }
            else
            {
                var certificate = input.Certificate;
                if (!_settings.Security.PrivateKeyExportable && input.CacheFile != null)
                {
                    certificate = new X509Certificate2(
                        input.CacheFile.FullName,
                        input.CacheFilePassword,
                        X509KeyStorageFlags.MachineKeySet |
                        X509KeyStorageFlags.PersistKeySet);
                }
                _log.Information("Installing certificate in the certificate store");
                InstallCertificate(certificate);
                if (_options.AclFullControl != null)
                {
                    SetAcl(certificate, _options.AclFullControl);
                }
                InstallCertificateChain(input.Chain);

            }
            input.StoreInfo.TryAdd(
                GetType(),
                new StoreInfo()
                {
                    Name = CertificateStoreOptions.PluginName,
                    Path = _store.Name
                });
            return Task.CompletedTask;
        }

        private void SetAcl(X509Certificate2 cert, List<string> fullControl)
        {
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
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to set requested ACL on private key");
            }
        }

        public Task Delete(CertificateInfo input)
        {
            _log.Information("Uninstalling certificate from the certificate store");
            UninstallCertificate(input.Certificate);
            return Task.CompletedTask;
        }

        public CertificateInfo? FindByThumbprint(string thumbprint) => ToInfo(GetCertificate(CertificateService.ThumbprintFilter(thumbprint)));

        private CertificateInfo? ToInfo(X509Certificate2? cert)
        {
            if (cert != null)
            {
                var ret = new CertificateInfo(cert);
                ret.StoreInfo.Add(
                    GetType(),
                    new StoreInfo()
                    {
                        Path = _store.Name
                    });
                return ret;
            }
            else
            {
                return null;
            }
        }

        private void InstallCertificate(X509Certificate2 certificate)
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

        private void InstallCertificateChain(List<X509Certificate2> chain)
        {
            X509Store imStore;
            try
            {
                imStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
                imStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                if (!imStore.IsOpen)
                {
                    _log.Verbose("Unable to open intermediate certificate authority store");
                    imStore = new X509Store(_storeName!, StoreLocation.LocalMachine);
                    imStore.Open(OpenFlags.ReadWrite);
                }
            }
            catch
            {
                _log.Warning("Error encountered while opening intermediate certificate store");
                return;
            }

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

        private void UninstallCertificate(X509Certificate2 certificate)
        {
            try
            {
                // Test if the user manually added the certificate to IIS
                if (_iisClient.HasWebSites)
                {
                    var hash = certificate.GetCertHash();
                    if (_iisClient.WebSites.Any(site =>
                        site.Bindings.Any(binding => 
                        StructuralComparisons.StructuralEqualityComparer.Equals(binding.CertificateHash, hash) &&
                        Equals(binding.CertificateStoreName, _storeName))))
                    {
                        _log.Error("The previous certificate was detected in IIS. Configure the IIS installation step to auto-update bindings.");
                        return;
                    }
                }
            } 
            catch
            {

            }
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

        private X509Certificate2? GetCertificate(Func<X509Certificate2, bool> filter)
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

        (bool, string?) IPlugin.Disabled => Disabled(_userRoleService);

        internal static (bool, string?) Disabled(IUserRoleService userRoleService)
        {
            if (userRoleService.IsAdmin) 
            {
                return (false, null);
            } 
            else 
            {
                return (true, "Run as administrator to allow certificate store access.");
            }
        }

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

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

        public void Dispose() => Dispose(true);

        #endregion
    }
}