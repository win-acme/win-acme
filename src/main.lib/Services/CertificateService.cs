using ACMESharp.Protocol;
using Newtonsoft.Json;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
            _cache = new DirectoryInfo(settingsService.Cache.Path);
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
                _log.Warning("Found {nr} files older than {days} days in cache path '{cachePath}'", count, days, _cache.FullName);
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
        void ICertificateService.Delete(Renewal renewal) => ClearCache(renewal);

        /// <summary>
        /// Encrypt or decrypt the cached private keys
        /// </summary>
        public void Encrypt()
        {
            foreach (var f in _cache.EnumerateFiles($"*.keys"))
            {
                var x = new ProtectedString(File.ReadAllText(f.FullName));
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
        public CertificateInfo? CachedInfo(Renewal renewal, Target? target = null)
        {
            var nameAll = GetPath(renewal, "");
            var directory = new DirectoryInfo(Path.GetDirectoryName(nameAll));
            var allPattern = Path.GetFileName(nameAll);
            var allFiles = directory.EnumerateFiles(allPattern + "*");
            if (!allFiles.Any())
            {
                return null;
            }

            FileInfo? fileCache = null;
            if (target != null)
            {
                var key = CacheKey(renewal, target);
                var keyName = Path.GetFileName(GetPath(renewal, $"-{key}{PfxPostFix}"));
                var keyFile = allFiles.Where(x => x.Name == keyName).FirstOrDefault();
                if (keyFile != null)
                {
                    fileCache = keyFile;
                }
                else
                {
                    var legacyFile = new FileInfo(GetPath(renewal, PfxPostFixLegacy));
                    if (legacyFile.Exists)
                    {
                        var legacyInfo = FromCache(legacyFile, renewal.PfxPassword?.Value);
                        if (Match(legacyInfo, target))
                        {
                            fileCache = legacyFile;
                        }
                    }
                }
            }
            else
            {
                fileCache = allFiles.OrderByDescending(x => x.LastWriteTime).FirstOrDefault();
            }

            if (fileCache != null)
            {
                try
                {
                    return FromCache(fileCache, renewal.PfxPassword?.Value);
                }
                catch
                {
                    // File corrupt or invalid password?
                    _log.Warning("Unable to read from certificate cache");
                }
            }
            return null;
        }

        /// <summary>
        /// See if the information in the certificate matches
        /// that of the specified target. Used to figure out whether
        /// or not the cache is out of date.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool Match(CertificateInfo info, Target target)
        {
            var identifiers = target.GetHosts(false);
            var idn = new IdnMapping();
            return info.CommonName == idn.GetAscii(target.CommonName) &&
                info.SanNames.Count == identifiers.Count() &&
                info.SanNames.All(h => identifiers.Contains(idn.GetAscii(h)));
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
        public string CacheKey(Renewal renewal, Target target)
        {
            // Check if we can reuse a cached certificate and/or order
            // based on currently active set of parameters and shape of 
            // the target.
            var cacheKeyBuilder = new StringBuilder();
            cacheKeyBuilder.Append(target.CommonName);
            cacheKeyBuilder.Append(string.Join(',', target.GetHosts(true).OrderBy(x => x).Select(x => x.ToLower())));
            _ = target.CsrBytes != null ?
                cacheKeyBuilder.Append(Convert.ToBase64String(target.CsrBytes)) :
                cacheKeyBuilder.Append("-");
            _ = renewal.CsrPluginOptions != null ?
                cacheKeyBuilder.Append(JsonConvert.SerializeObject(renewal.CsrPluginOptions)) :
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
        public async Task<CertificateInfo> RequestCertificate(
            ICsrPlugin? csrPlugin, 
            RunLevel runLevel, 
            Renewal renewal, 
            Target target,
            OrderDetails order)
        {
            // What are we going to get?
            var cacheKey = CacheKey(renewal, target);
            var pfxFileInfo = new FileInfo(GetPath(renewal, $"-{cacheKey}{PfxPostFix}"));

            // Determine/check the common name
            var identifiers = target.GetHosts(false);
            var commonNameUni = target.CommonName;
            var commonNameAscii = string.Empty;
            if (!string.IsNullOrWhiteSpace(commonNameUni))
            {
                var idn = new IdnMapping();
                commonNameAscii = idn.GetAscii(commonNameUni);
                if (!identifiers.Contains(commonNameAscii, StringComparer.InvariantCultureIgnoreCase))
                {
                    _log.Warning($"Common name {commonNameUni} provided is invalid.");
                    commonNameAscii = identifiers.First();
                    commonNameUni = idn.GetUnicode(commonNameAscii);
                }
            }

            // Determine the friendly name
            var friendlyNameBase = renewal.FriendlyName;
            if (string.IsNullOrEmpty(friendlyNameBase))
            {
                friendlyNameBase = target.FriendlyName;
            }
            if (string.IsNullOrEmpty(friendlyNameBase))
            {
                friendlyNameBase = commonNameUni;
            }
            var friendyName = $"{friendlyNameBase} @ {_inputService.FormatDate(DateTime.Now)}";

            // Try using cached certificate first to avoid rate limiting during
            // (initial?) deployment troubleshooting. Real certificate requests
            // will only be done once per day maximum unless the --force parameter 
            // is used.
            var cache = CachedInfo(renewal, target);
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
                            friendlyNameBase,
                            _settings.Cache.ReuseDays,
                            nameof(MainArguments.Force).ToLower());
                        return cache;
                    }
                }
            }

            if (order.Payload.Status != AcmeClient.OrderValid)
            {
                // Clear cache and write new cert
                ClearCache(renewal, postfix: CsrPostFix);

                if (target.CsrBytes == null)
                {
                    if (csrPlugin == null)
                    {
                        throw new InvalidOperationException("Missing csrPlugin");
                    }
                    var keyFile = GetPath(renewal, ".keys");
                    var csr = await csrPlugin.GenerateCsr(keyFile, commonNameAscii, identifiers);
                    var keySet = await csrPlugin.GetKeys();
                    target.CsrBytes = csr.GetDerEncoded();
                    target.PrivateKey = keySet.Private;
                    var csrPath = GetPath(renewal, CsrPostFix);
                    File.WriteAllText(csrPath, _pemService.GetPem("CERTIFICATE REQUEST", target.CsrBytes));
                    _log.Debug("CSR stored at {path} in certificate cache folder {folder}", Path.GetFileName(csrPath), Path.GetDirectoryName(csrPath));

                }

                _log.Verbose("Submitting CSR");
                order = await _client.SubmitCsr(order, target.CsrBytes);
                if (order.Payload.Status != AcmeClient.OrderValid)
                {
                    _log.Error("Unexpected order status {status}", order.Payload.Status);
                    throw new Exception($"Unable to complete order");
                }
            }

            _log.Information("Requesting certificate {friendlyName}", friendlyNameBase);
            var rawCertificate = await _client.GetCertificate(order);
            if (rawCertificate == null)
            {
                throw new Exception($"Unable to get certificate");
            }

            // Build pfx archive including any intermediates provided
            var text = Encoding.UTF8.GetString(rawCertificate);
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
                        friendyName :
                        bcCertificate.SubjectDN.ToString();
                    pfx.SetCertificateEntry(bcCertificateAlias, bcCertificateEntry);

                    // Assume that the first certificate in the reponse is the main one
                    // so we associate the private key with that one. Other certificates
                    // are intermediates
                    if (startIndex == 0 && target.PrivateKey != null)
                    {
                        var bcPrivateKeyEntry = new bc.Pkcs.AsymmetricKeyEntry(target.PrivateKey);
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

            ClearCache(renewal, postfix: $"*{PfxPostFix}");
            ClearCache(renewal, postfix: $"*{PfxPostFixLegacy}");
            File.WriteAllBytes(pfxFileInfo.FullName, tempPfx.Export(X509ContentType.Pfx, renewal.PfxPassword?.Value));
            _log.Debug("Certificate written to cache file {path} in certificate cache folder {folder}. It will be " +
                "reused when renewing within {x} day(s) as long as the Target and Csr parameters remain the same and " +
                "the --force switch is not used.", 
                pfxFileInfo.Name, 
                pfxFileInfo.Directory.FullName,
                _settings.Cache.ReuseDays);

            if (csrPlugin != null)
            {
                try
                {
                    var cert = tempPfx.
                        OfType<X509Certificate2>().
                        Where(x => x.HasPrivateKey).
                        FirstOrDefault();
                    if (cert != null)
                    {
                        var certIndex = tempPfx.IndexOf(cert);
                        var newVersion = await csrPlugin.PostProcess(cert);
                        if (newVersion != cert)
                        {
                            newVersion.FriendlyName = friendyName;
                            tempPfx[certIndex] = newVersion;
                            File.WriteAllBytes(pfxFileInfo.FullName, tempPfx.Export(X509ContentType.Pfx, renewal.PfxPassword?.Value));
                            newVersion.Dispose();
                        }
                    }
                }
                catch (Exception)
                {
                    _log.Warning("Private key conversion error.");
                }
            }

            pfxFileInfo.Refresh();

            // Update LastFriendlyName so that the user sees
            // the most recently issued friendlyName in
            // the WACS GUI
            renewal.LastFriendlyName = friendlyNameBase;

            // Recreate X509Certificate2 with correct flags for Store/Install
            return FromCache(pfxFileInfo, renewal.PfxPassword?.Value);
        }

        private CertificateInfo FromCache(FileInfo pfxFileInfo, string? password)
        {
            var rawCollection = ReadAsCollection(pfxFileInfo, password);
            var list = rawCollection.OfType<X509Certificate2>().ToList();
            // Get first certificate that has not been used to issue 
            // another one in the collection. That is the outermost leaf.
            var main = list.FirstOrDefault(x => !list.Any(y => x.Subject == y.Issuer));
            list.Remove(main);
            var lastChainElement = main;
            var orderedCollection = new List<X509Certificate2>();
            while (list.Count > 0)
            {
                var signedBy = list.FirstOrDefault(x => main.Issuer == x.Subject);
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
            var info = CachedInfo(renewal);
            if (info != null)
            {
                var certificateDer = info.Certificate.Export(X509ContentType.Cert);
                await _client.RevokeCertificate(certificateDer);
            }
            ClearCache(renewal);
            _log.Warning("Certificate for {target} revoked, you should renew immediately", renewal);
        }

        /// <summary>
        /// Common filter for different store plugins
        /// </summary>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public static Func<X509Certificate2, bool> ThumbprintFilter(string thumbprint) => new Func<X509Certificate2, bool>(x => string.Equals(x.Thumbprint, thumbprint));

    }
}
