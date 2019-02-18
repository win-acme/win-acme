using ACMESharp.Protocol;
using PKISharp.WACS.Acme;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Properties;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using bc = Org.BouncyCastle;

namespace PKISharp.WACS.Services
{
    internal class CertificateService
    {
        private ILogService _log;
        private AcmeClient _client;
        private readonly RunLevel _runLevel;
        private readonly ProxyService _proxy;
        private readonly DirectoryInfo _cache;

        public CertificateService(
            ILogService log,
            RunLevel runLevel,
            AcmeClient client,
            ProxyService proxy,
            ISettingsService settingsService)
        {
            _log = log;
            _client = client;
            _runLevel = runLevel;
            _cache = new DirectoryInfo(settingsService.CertificatePath);
            _proxy = proxy;
            CheckStaleFiles();
        }

        /// <summary>
        /// List all files older than 120 days from the certificate
        /// cache, because that means that the certificates have been
        /// expired for 30 days. User might want to clean them up
        /// </summary>
        public void CheckStaleFiles()
        {
            var days = 120;
            var count = _cache.
                GetFiles().
                Where(x => x.LastWriteTime < DateTime.Now.AddDays(-days)).
                Count();
            if (count > 0)
            {
                _log.Warning("Found {nr} files older than {days} days in the CertificatePath", count, days);
            }
        }

        /// <summary>
        /// Delete cached files related to a specific renewal
        /// </summary>
        /// <param name="renewal"></param>
        private void ClearCache(Renewal renewal)
        {
            foreach (var f in _cache.GetFiles($"*{renewal.Id}*"))
            {
                _log.Verbose("Deleting {file} from cache", f.Name);
                f.Delete();
            }
        }

        /// <summary>
        /// Find local certificate file based on naming conventions
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="postfix"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        private string GetPath(Renewal renewal, string postfix, string prefix = "")
        {
            return Path.Combine(_cache.FullName, $"{prefix}{renewal.Id}{postfix}");
        }

        /// <summary>
        /// Helper function for PEM encoding
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal string GetPem(object obj)
        {
            string pem;
            using (var tw = new StringWriter())
            {
                var pw = new bc.OpenSsl.PemWriter(tw);
                pw.WriteObject(obj);
                pem = tw.GetStringBuilder().ToString();
                tw.GetStringBuilder().Clear();
            }
            return pem;
        }
        internal string GetPem(string name, byte[] content) => GetPem(new bc.Utilities.IO.Pem.PemObject(name, content));

        /// <summary>
        /// Helper function for reading PEM encoding
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pem"></param>
        /// <returns></returns>
        private T ParsePem<T>(string pem)
        {
            using (var tr = new StringReader(pem))
            {
                var pr = new bc.OpenSsl.PemReader(tr);
                return (T)pr.ReadObject();
            }
        }

        /// <summary>
        /// Read from the disk cache
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public CertificateInfo CachedInfo(Renewal renewal)
        {
            var pfxFileInfo = new FileInfo(PfxFilePath(renewal));
            if (pfxFileInfo.Exists)
            {
                try
                {
                    return new CertificateInfo()
                    {
                        Certificate = ReadForUse(pfxFileInfo, renewal.PfxPassword),
                        PfxFile = pfxFileInfo,
                        PfxFilePassword = renewal.PfxPassword
                    };
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
        /// Request certificate from the ACME server
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        public CertificateInfo RequestCertificate(ICsrPlugin csrPlugin, Renewal renewal, Target target, OrderDetails order)
        {
            // What are we going to get?
            var pfxFileInfo = new FileInfo(PfxFilePath(renewal));

            // Determine/check the common name
            var identifiers = target.GetHosts(false);
            var commonName = target.CommonName;
            if (!string.IsNullOrWhiteSpace(commonName))
            {
                var idn = new IdnMapping();
                commonName = idn.GetAscii(commonName);
                if (!identifiers.Contains(commonName, StringComparer.InvariantCultureIgnoreCase))
                {
                    _log.Warning($"Common name {commonName} provided is invalid.");
                    commonName = identifiers.First();
                }
            }

            // Determine the friendly name
            var friendlyName = renewal.FriendlyName;
            if (string.IsNullOrEmpty(friendlyName))
            {
                friendlyName = target.FriendlyName;
            }
            if (string.IsNullOrEmpty(friendlyName))
            {
                friendlyName = commonName;
            }

            // Try using cached certificate first to avoid rate limiting during
            // (initial?) deployment troubleshooting. Real certificate requests
            // will only be done once per day maximum unless the --force parameter 
            // is used.
            var cache = CachedInfo(renewal);
            if (cache != null && 
                cache.PfxFile.LastWriteTime > DateTime.Now.AddDays(-1) &&
                cache.Match(target))
            {
                if (_runLevel.HasFlag(RunLevel.Force))
                {
                    _log.Warning("Cached certificate available but not used with --{switch}. Use 'Renew specific' or " +
                        "'Renew all' in the main menu to run unscheduled renewals without hitting rate limits.", 
                        nameof(MainArguments.Force).ToLower());
                }
                else
                {
                    _log.Warning("Using cached certificate for {friendlyName}. To force issue of a new certificate within " +
                        "24 hours, delete the .pfx file from the CertificatePath or run with the --{switch} switch. " +
                        "Be ware that you might run into rate limits doing so.", 
                        friendlyName, 
                        nameof(MainArguments.Force).ToLower());
                    return cache;
                }
            }
          
            var csr = csrPlugin.GenerateCsr(commonName, identifiers);
            var csrBytes = csr.CreateSigningRequest();
            order = _client.SubmitCsr(order, csrBytes);
            File.WriteAllText(GetPath(renewal, "-csr.pem"), GetPem("CERTIFICATE REQUEST", csrBytes));

            _log.Information("Requesting certificate {friendlyName}", friendlyName);
            var rawCertificate = _client.GetCertificate(order);
            if (rawCertificate == null)
            {
                throw new Exception($"Unable to get certificate");
            }

            var certificate = new X509Certificate2(rawCertificate);
            var certificateExport = certificate.Export(X509ContentType.Cert);
            var crtPem = GetPem("CERTIFICATE", certificateExport);

            // Get issuer certificate 
            var chain = new X509Chain();
            chain.Build(certificate);
            X509Certificate2 issuerCertificate = chain.ChainElements[1].Certificate;
            var issuerCertificateExport = issuerCertificate.Export(X509ContentType.Cert);
            var issuerPem = GetPem("CERTIFICATE", issuerCertificateExport);
          
            // Build pfx archive
            var pfx = new bc.Pkcs.Pkcs12Store();
            var bcCertificate = ParsePem<bc.X509.X509Certificate>(crtPem);
            var bcCertificateEntry = new bc.Pkcs.X509CertificateEntry(bcCertificate);
            var bcCertificateAlias = bcCertificate.SubjectDN.ToString();
            var bcPrivateKeyEntry = new bc.Pkcs.AsymmetricKeyEntry(csrPlugin.GetPrivateKey());
            pfx.SetCertificateEntry(bcCertificateAlias, bcCertificateEntry);
            pfx.SetKeyEntry(bcCertificateAlias, bcPrivateKeyEntry, new[] { bcCertificateEntry });

            var bcIssuer = ParsePem<bc.X509.X509Certificate>(issuerPem);
            var bcIssuerEntry = new bc.Pkcs.X509CertificateEntry(bcIssuer);
            var bcIssuerAlias = bcIssuer.SubjectDN.ToString();
            pfx.SetCertificateEntry(bcIssuerAlias, bcIssuerEntry);
           
            var pfxStream = new MemoryStream();
            pfx.Save(pfxStream, null, new bc.Security.SecureRandom());
            pfxStream.Position = 0;
            using (var pfxStreamReader = new BinaryReader(pfxStream))
            {
                var tempPfx = new X509Certificate2(
                    pfxStreamReader.ReadBytes((int)pfxStream.Length),
                    (string)null,
                    X509KeyStorageFlags.MachineKeySet |
                    X509KeyStorageFlags.PersistKeySet |
                    X509KeyStorageFlags.Exportable);
                if (csrPlugin.CanConvert())
                {
                    try
                    {
                        var converted = csrPlugin.Convert(tempPfx.PrivateKey);
                        if (converted != null)
                        {
                            tempPfx.PrivateKey = converted;
                        }
                    }
                    catch
                    {
                        _log.Warning("Private key conversion error.");
                    }
                }
                   
                tempPfx.FriendlyName = $"{friendlyName} {DateTime.Now.ToUserString()}";
                File.WriteAllBytes(pfxFileInfo.FullName, tempPfx.Export(X509ContentType.Pfx, renewal.PfxPassword));
                pfxFileInfo.Refresh();
            }

            // Update LastFriendlyName so that the user sees
            // the most recently issued friendlyName in
            // the WACS GUI
            renewal.LastFriendlyName = friendlyName;

            // Recreate X509Certificate2 with correct flags for Store/Install
            return new CertificateInfo()
            {
                Certificate = ReadForUse(pfxFileInfo, renewal.PfxPassword),
                PfxFile = pfxFileInfo,
                PfxFilePassword = renewal.PfxPassword
            };
        }

        /// <summary>
        /// Read certificate for it to be exposed to the StorePlugin and InstallationPlugins
        /// </summary>
        /// <param name="source"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private X509Certificate2 ReadForUse(FileInfo source, string password)
        {
            // Flags used for the X509Certificate2 as 
            var externalFlags =
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet;
            if (Settings.Default.PrivateKeyExportable)
            {
                _log.Verbose("Set private key exportable");
                externalFlags |= X509KeyStorageFlags.Exportable;
            }
            return new X509Certificate2(source.FullName, password, externalFlags);
        }

        /// <summary>
        /// Revoke previously issued certificate
        /// </summary>
        /// <param name="binding"></param>
        public void RevokeCertificate(Renewal renewal)
        {
            // Delete cached files
            var info = CachedInfo(renewal);
            if (info != null)
            {
                var certificateDer = info.Certificate.Export(X509ContentType.Cert);
                //_client.RevokeCertificate();
            }
            ClearCache(renewal);
            _log.Warning("Certificate for {target} revoked, you should renew immediately", renewal);
        }

        /// <summary>
        /// Path to the cached PFX file
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public string PfxFilePath(Renewal renewal)
        {
            return GetPath(renewal, "-cache.pfx", "");
        }

        /// <summary>
        /// Common filter for Central SSL and Certificate Store
        /// </summary>
        /// <param name="friendlyName"></param>
        /// <returns></returns>
        public static Func<X509Certificate2, bool> ThumbprintFilter(string thumbprint)
        {
            return new Func<X509Certificate2, bool>(x => string.Equals(x.Thumbprint, thumbprint));
        }
    }
}
