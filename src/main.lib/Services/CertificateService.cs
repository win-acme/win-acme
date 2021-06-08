using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using bc = Org.BouncyCastle;

namespace PKISharp.WACS.Services
{
    internal class CertificateService : ICertificateService
    {
        private const string CsrPostFix = "-csr.pem";
        private const string PfxPostFix = "-temp.pfx";
        private const string PfxPostFixLegacy = "-cache.pfx";

        private readonly IInputService _inputService;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly AcmeClient _client;
        private readonly DirectoryInfo _cache;
        private readonly PemService _pemService;

        public CertificateService(
            ILogService log,
            AcmeClient client,
            PemService pemService,
            IInputService inputService,
            ISettingsService settingsService)
        {
            _log = log;
            _client = client;
            _pemService = pemService;
            _cache = new DirectoryInfo(settingsService.Cache.Path!);
            _settings = settingsService;
            _inputService = inputService;
            CheckStaleFiles();
        }

        /// <summary>
        /// List all files older than 120 days from the certificate
        /// cache, because that means that the certificates have been
        /// expired for 30 days. User might want to clean them up
        /// </summary>
        private void CheckStaleFiles()
        {
            var days = 120;
            var files = _cache.
                GetFiles().
                Where(x => x.LastWriteTime < DateTime.Now.AddDays(-days));
            var count = files.Count();
            if (count > 0)
            {
                _log.Warning("Found {nr} files older than {days} days in {cachePath}", count, days, _cache.FullName);
                if (_settings.Cache.DeleteStaleFiles)
                {
                    _log.Information("Deleting stale files");
                    try
                    {
                        foreach (var file in files)
                        {
                            file.Delete();
                        }
                        _log.Information("Stale files deleted");
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Deleting stale files");
                    }
                }
            }
        }

        /// <summary>
        /// Delete cached files related to a specific renewal
        /// </summary>
        /// <param name="renewal"></param>
        private void ClearCache(Renewal renewal, string prefix = "*", string postfix = "*") 
        {
            foreach (var f in _cache.EnumerateFiles($"{prefix}{renewal.Id}{postfix}"))
            {
                if (f.LastWriteTime < DateTime.Now.AddDays(_settings.Cache.ReuseDays * -1))
                {
                    _log.Verbose("Deleting {file} from {folder}", f.Name, _cache.FullName);
                    try
                    {
                        f.Delete();
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("Error deleting {file} from {folder}: {message}", f.Name, _cache.FullName, ex.Message);
                    }
                }
            }
        }
        void ICertificateService.Delete(Renewal renewal) => ClearCache(renewal);

        /// <summary>
        /// Encrypt or decrypt the cached private keys
        /// </summary>
        public void Encrypt()
        {
            foreach (var f in _cache.EnumerateFiles($"*.keys"))
            {
                var x = new ProtectedString(File.ReadAllText(f.FullName), _log);
                _log.Information("Rewriting {x}", f.Name);
                File.WriteAllText(f.FullName, x.DiskValue(_settings.Security.EncryptConfig));
            }
        }

        /// <summary>
        /// Find local certificate file based on naming conventions
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="postfix"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private string GetPath(Renewal renewal, string postfix, string prefix = "") => Path.Combine(_cache.FullName, $"{prefix}{renewal.Id}{postfix}");

        /// <summary>
        /// Read from the disk cache
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public CertificateInfo? CachedInfo(Order order)
        {
            var cachedInfos = CachedInfos(order.Renewal);
            if (!cachedInfos.Any())
            {
                return null;
            }

            var keyName = GetPath(order.Renewal, $"-{CacheKey(order)}{PfxPostFix}");
            var fileCache = cachedInfos.Where(x => x.CacheFile?.FullName == keyName).FirstOrDefault();
            if (fileCache == null)
            {
                var legacyFile = GetPath(order.Renewal, PfxPostFixLegacy);
                var candidate = cachedInfos.Where(x => x.CacheFile?.FullName == legacyFile).FirstOrDefault();
                if (candidate != null)
                {
                    if (Match(candidate, order.Target))
                    {
                        fileCache = candidate;
                    }
                }
            }
            return fileCache;
        }

        public IEnumerable<CertificateInfo> CachedInfos(Renewal renewal)
        {
            var ret = new List<CertificateInfo>();
            var nameAll = GetPath(renewal, "*.pfx");
            var directory = new DirectoryInfo(Path.GetDirectoryName(nameAll)!);
            var allPattern = Path.GetFileName(nameAll);
            var allFiles = directory.EnumerateFiles(allPattern + "*");
            var fileCache = allFiles.OrderByDescending(x => x.LastWriteTime);
            foreach (var file in fileCache)
            {
                try
                {
                    ret.Add(FromCache(file, renewal.PfxPassword?.Value));
                }
                catch
                {
                    // File corrupt or invalid password?
                    _log.Warning("Unable to read {i} from certificate cache", file.Name);
                }
            }
            return ret;
        }

        /// <summary>
        /// See if the information in the certificate matches
        /// that of the specified target. Used to figure out whether
        /// or not the cache is out of date.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private static bool Match(CertificateInfo info, Target target)
        {
            var identifiers = target.GetIdentifiers(false);
            return info.CommonName == target.CommonName.Unicode(false) &&
                info.SanNames.Count == identifiers.Count &&
                info.SanNames.All(h => identifiers.Contains(h.Unicode(false)));
        }

        /// <summary>
        /// To check if it's possible to reuse a previously retrieved
        /// certificate we create a hash of its key properties and included
        /// that hash in the file name. If we get the same hash on a 
        /// subsequent run, it means it's safe to reuse (no relevant changes).
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public string CacheKey(Order order)
        {
            // Check if we can reuse a cached certificate and/or order
            // based on currently active set of parameters and shape of 
            // the target.
            var cacheKeyBuilder = new StringBuilder();
            cacheKeyBuilder.Append(order.CacheKeyPart);
            cacheKeyBuilder.Append(order.Target.CommonName);
            cacheKeyBuilder.Append(string.Join(',', order.Target.GetIdentifiers(true).OrderBy(x => x).Select(x => x.Value.ToLower())));
            _ = order.Target.CsrBytes != null ?
                cacheKeyBuilder.Append(Convert.ToBase64String(order.Target.CsrBytes)) :
                cacheKeyBuilder.Append("-");
            _ = order.Renewal.CsrPluginOptions != null ?
                cacheKeyBuilder.Append(JsonConvert.SerializeObject(order.Renewal.CsrPluginOptions)) :
                cacheKeyBuilder.Append("-");
            return cacheKeyBuilder.ToString().SHA1();
        }

        /// <summary>
        /// Request certificate from the ACME server
        /// </summary>
        /// <param name="csrPlugin">Plugin used to generate CSR if it has not been provided in the target</param>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<CertificateInfo> RequestCertificate(ICsrPlugin? csrPlugin, RunLevel runLevel, Order order)
        {
            if (order.Details == null)
            {
                throw new InvalidOperationException("No order details found");
            }

            // What are we going to get?
            var cacheKey = CacheKey(order);
            var pfxFileInfo = new FileInfo(GetPath(order.Renewal, $"-{cacheKey}{PfxPostFix}"));

            // Determine/check the common name
            var identifiers = order.Target.GetIdentifiers(false);
            var commonName = order.Target.CommonName;
            if (!identifiers.Contains(commonName.Unicode(false)))
            {
                _log.Warning($"Common name {commonName.Value} provided is invalid.");
                commonName = identifiers.First();
            }

            // Determine the friendly name base (for the renewal)
            var friendlyNameBase = order.Renewal.FriendlyName;
            if (string.IsNullOrEmpty(friendlyNameBase))
            {
                friendlyNameBase = order.Target.FriendlyName;
            }
            if (string.IsNullOrEmpty(friendlyNameBase))
            {
                friendlyNameBase = commonName.Unicode(true).Value;
            }

            // Determine the friendly name for this specific certificate
            var friendlyNameIntermediate = friendlyNameBase;
            if (!string.IsNullOrEmpty(order.FriendlyNamePart))
            {
                friendlyNameIntermediate += $" [{order.FriendlyNamePart}]";
            }
            var friendlyName = $"{friendlyNameIntermediate} @ {_inputService.FormatDate(DateTime.Now)}";

            // Try using cached certificate first to avoid rate limiting during
            // (initial?) deployment troubleshooting. Real certificate requests
            // will only be done once per day maximum unless the --force parameter 
            // is used.
            var cache = CachedInfo(order);
            if (cache != null && cache.CacheFile != null)
            {
                if (cache.CacheFile.LastWriteTime > DateTime.Now.AddDays(_settings.Cache.ReuseDays * -1))
                {
                    if (runLevel.HasFlag(RunLevel.IgnoreCache))
                    {
                        _log.Warning("Cached certificate available on disk but not used due to --{switch} switch.", 
                            nameof(MainArguments.Force).ToLower());
                    }
                    else
                    {
                        _log.Warning("Using cached certificate for {friendlyName}. To force a new request of the " +
                            "certificate within {days} days, run with the --{switch} switch.",
                            friendlyNameIntermediate,
                            _settings.Cache.ReuseDays,
                            nameof(MainArguments.Force).ToLower());
                        return cache;
                    }
                }
            }

            if (order.Details.Payload.Status != AcmeClient.OrderValid)
            {
                // Clear cache and write new cert
                ClearCache(order.Renewal, postfix: CsrPostFix);

                if (order.Target.CsrBytes == null)
                {
                    if (csrPlugin == null)
                    {
                        throw new InvalidOperationException("Missing csrPlugin");
                    }
                    // Backwards compatible with existing keys, which are not split per order yet.
                    var keyFile = new FileInfo(GetPath(order.Renewal, $".keys"));
                    if (!keyFile.Exists)
                    {
                        keyFile = new FileInfo(GetPath(order.Renewal, $"-{cacheKey}.keys"));
                    }
                    var csr = await csrPlugin.GenerateCsr(keyFile.FullName, commonName, identifiers);
                    var keySet = await csrPlugin.GetKeys();
                    order.Target.CsrBytes = csr.GetDerEncoded();
                    order.Target.PrivateKey = keySet.Private;
                    var csrPath = GetPath(order.Renewal, $"-{cacheKey}{CsrPostFix}");
                    await File.WriteAllTextAsync(csrPath, _pemService.GetPem("CERTIFICATE REQUEST", order.Target.CsrBytes));
                    _log.Debug("CSR stored at {path} in certificate cache folder {folder}", Path.GetFileName(csrPath), Path.GetDirectoryName(csrPath));

                }

                _log.Verbose("Submitting CSR");
                order.Details = await _client.SubmitCsr(order.Details, order.Target.CsrBytes);
                if (order.Details.Payload.Status != AcmeClient.OrderValid)
                {
                    _log.Error("Unexpected order status {status}", order.Details.Payload.Status);
                    throw new Exception($"Unable to complete order");
                }
            }

            _log.Information("Requesting certificate {friendlyName}", friendlyNameIntermediate);
            var certInfo = await _client.GetCertificate(order.Details);
            if (certInfo == null || certInfo.Certificate == null)
            {
                throw new Exception($"Unable to get certificate");
            }
            var alternatives = new List<X509Certificate2Collection>
            {
                ParseCertificate(certInfo.Certificate, friendlyName, order.Target.PrivateKey)
            };
            if (certInfo.Links != null)
            {
                foreach (var alt in certInfo.Links["alternate"])
                {
                    try
                    {
                        var altCertRaw = await _client.GetCertificate(alt);
                        var altCert = ParseCertificate(altCertRaw, friendlyName, order.Target.PrivateKey);
                        alternatives.Add(altCert);
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("Unable to get alternate certificate: {ex}", ex.Message);
                    }
                }
            }
            var selected = Select(alternatives);

            ClearCache(order.Renewal, postfix: $"*{PfxPostFix}");
            ClearCache(order.Renewal, postfix: $"*{PfxPostFixLegacy}");
            await File.WriteAllBytesAsync(pfxFileInfo.FullName, selected.Export(X509ContentType.Pfx, order.Renewal.PfxPassword?.Value)!);
            _log.Debug("Certificate written to cache file {path} in certificate cache folder {folder}. It will be " +
                "reused when renewing within {x} day(s) as long as the --source and --csr parameters remain the same and " +
                "the --force switch is not used.", 
                pfxFileInfo.Name, 
                pfxFileInfo.Directory!.FullName,
                _settings.Cache.ReuseDays);

            if (csrPlugin != null)
            {
                try
                {
                    var cert = selected.
                        OfType<X509Certificate2>().
                        Where(x => x.HasPrivateKey).
                        FirstOrDefault();
                    if (cert != null)
                    {
                        var certIndex = selected.IndexOf(cert);
                        var newVersion = await csrPlugin.PostProcess(cert);
                        if (newVersion != cert)
                        {
                            newVersion.FriendlyName = friendlyName;
                            selected[certIndex] = newVersion;
                            await File.WriteAllBytesAsync(pfxFileInfo.FullName, selected.Export(X509ContentType.Pfx, order.Renewal.PfxPassword?.Value)!);
                            newVersion.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning("Private key conversion error: {ex}", ex.Message);
                }
            }

            pfxFileInfo.Refresh();

            // Update LastFriendlyName so that the user sees
            // the most recently issued friendlyName in
            // the WACS GUI
            order.Renewal.LastFriendlyName = friendlyNameBase;

            // Recreate X509Certificate2 with correct flags for Store/Install
            return FromCache(pfxFileInfo, order.Renewal.PfxPassword?.Value);
        }

        /// <summary>
        /// Get the name for the root issuer
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        private string Root(X509Certificate2Collection option) => new CertificateInfo(option[0]).Issuer;

        /// <summary>
        /// Choose between different versions of the certificate
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        private X509Certificate2Collection Select(List<X509Certificate2Collection> options)
        {
            var selected = options[0];
            if (options.Count() > 1)
            {

                _log.Debug("Found {n} version(s) of the certificate", options.Count);
                foreach (var option in options)
                {
                    _log.Debug("Option {n} issued by {issuer} (thumb: {thumb})", options.IndexOf(option) + 1, Root(option), option[0].Thumbprint);
                }
                if (!string.IsNullOrEmpty(_settings.Acme.PreferredIssuer))
                {
                    var match = options.FirstOrDefault(x => string.Equals(Root(x), _settings.Acme.PreferredIssuer, StringComparison.InvariantCultureIgnoreCase));
                    if (match != null)
                    {
                        selected = match;
                    }
                } 
                _log.Debug("Selected option {n}", options.IndexOf(selected) + 1);
            }
            if (!string.IsNullOrEmpty(_settings.Acme.PreferredIssuer) && 
                !string.Equals(Root(selected), _settings.Acme.PreferredIssuer, StringComparison.InvariantCultureIgnoreCase))
            {
                _log.Warning("Unable to find certificate issued by preferred issuer {issuer}", _settings.Acme.PreferredIssuer);
            }
            return selected;
        }

        /// <summary>
        /// Parse bytes to a usable certificate
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="friendlyName"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        private X509Certificate2Collection ParseCertificate(byte[] bytes, string friendlyName, AsymmetricKeyParameter? pk)
        {
            // Build pfx archive including any intermediates provided
            var text = Encoding.UTF8.GetString(bytes);
            var pfx = new bc.Pkcs.Pkcs12Store();
            var startIndex = 0;
            var endIndex = 0;
            const string startString = "-----BEGIN CERTIFICATE-----";
            const string endString = "-----END CERTIFICATE-----";
            while (true)
            {
                startIndex = text.IndexOf(startString, startIndex);
                if (startIndex < 0)
                {
                    break;
                }
                endIndex = text.IndexOf(endString, startIndex);
                if (endIndex < 0)
                {
                    break;
                }
                endIndex += endString.Length;
                var pem = text[startIndex..endIndex];
                var bcCertificate = _pemService.ParsePem<bc.X509.X509Certificate>(pem);
                if (bcCertificate != null)
                {
                    var bcCertificateEntry = new bc.Pkcs.X509CertificateEntry(bcCertificate);
                    var bcCertificateAlias = startIndex == 0 ?
                        friendlyName :
                        bcCertificate.SubjectDN.ToString();
                    pfx.SetCertificateEntry(bcCertificateAlias, bcCertificateEntry);

                    // Assume that the first certificate in the reponse is the main one
                    // so we associate the private key with that one. Other certificates
                    // are intermediates
                    if (startIndex == 0 && pk != null)
                    {
                        var bcPrivateKeyEntry = new bc.Pkcs.AsymmetricKeyEntry(pk);
                        pfx.SetKeyEntry(bcCertificateAlias, bcPrivateKeyEntry, new[] { bcCertificateEntry });
                    }
                }
                else
                {
                    _log.Warning("PEM data from index {0} to {1} could not be parsed as X509Certificate", startIndex, endIndex);
                }

                startIndex = endIndex;
            }

            var pfxStream = new MemoryStream();
            pfx.Save(pfxStream, null, new bc.Security.SecureRandom());
            pfxStream.Position = 0;
            using var pfxStreamReader = new BinaryReader(pfxStream);

            var tempPfx = new X509Certificate2Collection();
            tempPfx.Import(
                pfxStreamReader.ReadBytes((int)pfxStream.Length),
                null,
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable);
            return tempPfx;
        }

        /// <summary>
        /// Load certificate information from cache
        /// </summary>
        /// <param name="pfxFileInfo"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private CertificateInfo FromCache(FileInfo pfxFileInfo, string? password)
        {
            var rawCollection = ReadAsCollection(pfxFileInfo, password);
            var list = rawCollection.OfType<X509Certificate2>().ToList();
            // Get first certificate that has not been used to issue 
            // another one in the collection. That is the outermost leaf.
            var main = list.FirstOrDefault(x => !list.Any(y => x.Subject == y.Issuer));
            if (main == null)
            {
                throw new Exception("No certificates found in pfx archive");
            }
            list.Remove(main);
            var lastChainElement = main;
            var orderedCollection = new List<X509Certificate2>();
            while (list.Count > 0)
            {
                var signedBy = list.FirstOrDefault(x => lastChainElement.Issuer == x.Subject);
                if (signedBy == null)
                {
                    // Chain cannot be resolved any further
                    break;
                }
                orderedCollection.Add(signedBy);
                lastChainElement = signedBy;
                list.Remove(signedBy);
            }
            return new CertificateInfo(main)
            {
                Chain = orderedCollection,
                CacheFile = pfxFileInfo,
                CacheFilePassword = password
            };
        }

        /// <summary>
        /// Read certificate for it to be exposed to the StorePlugin and InstallationPlugins
        /// </summary>
        /// <param name="source"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private X509Certificate2Collection ReadAsCollection(FileInfo source, string? password)
        {
            // Flags used for the X509Certificate2 as 
            var externalFlags =
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet |
                X509KeyStorageFlags.Exportable;
            var ret = new X509Certificate2Collection();
            ret.Import(source.FullName, password, externalFlags);
            return ret;
        }

        /// <summary>
        /// Revoke previously issued certificate
        /// </summary>
        /// <param name="binding"></param>
        public async Task RevokeCertificate(Renewal renewal)
        {
            // Delete cached files
            var infos = CachedInfos(renewal);
            foreach (var info in infos)
            {
                try
                {
                    var certificateDer = info.Certificate.Export(X509ContentType.Cert);
                    await _client.RevokeCertificate(certificateDer);
                    info.CacheFile?.Delete();
                    _log.Warning($"Revoked certificate {info.Certificate.FriendlyName}");
                } 
                catch (Exception ex)
                {
                    _log.Error(ex, $"Error revoking certificate {info.Certificate.FriendlyName}, you may retry");
                }
            }
        }

        /// <summary>
        /// Common filter for different store plugins
        /// </summary>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public static Func<X509Certificate2, bool> ThumbprintFilter(string thumbprint) => new Func<X509Certificate2, bool>(x => string.Equals(x.Thumbprint, thumbprint));
    }
}
