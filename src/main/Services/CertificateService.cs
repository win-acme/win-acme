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
        private readonly string _certificatePath;

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
            _certificatePath = settingsService.CertificatePath;
            _proxy = proxy;
        }

        /// <summary>
        /// Find local certificate file based on naming conventions
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="postfix"></param>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public string GetPath(Renewal renewal, string postfix, string prefix = "")
        {
            return Path.Combine(_certificatePath, $"{prefix}{renewal.Id}{postfix}");
        }

        /// <summary>
        /// Helper function for PEM encoding
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private string GetPem(object obj)
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
        private string GetPem(string name, byte[] content) => GetPem(new bc.Utilities.IO.Pem.PemObject(name, content));

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
                        PfxFile = pfxFileInfo
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
                        "Be ware that you might run into rate limits doing so.", renewal.FriendlyName, 
                        nameof(MainArguments.Force).ToLower());
                    return cache;
                }
            }
          
            var csr = csrPlugin.GenerateCsr(commonName, identifiers);
            var csrBytes = csr.CreateSigningRequest();
            order = _client.SubmitCsr(order, csrBytes);

            if (Settings.Default.SavePrivateKeyPem)
            {
                File.WriteAllText(GetPath(renewal, "-key.pem"), GetPem(csrPlugin.GeneratePrivateKey()));
            }
            File.WriteAllText(GetPath(renewal, "-csr.pem"), GetPem("CERTIFICATE REQUEST", csrBytes));

            _log.Information("Requesting certificate {friendlyName}", renewal.FriendlyName);
            var rawCertificate = _client.GetCertificate(order);
            if (rawCertificate == null)
            {
                throw new Exception($"Unable to get certificate");
            }

            var certificate = new X509Certificate2(rawCertificate);
            var certificateExport = certificate.Export(X509ContentType.Cert);

            var crtDerFile = GetPath(renewal, $"-crt.der");
            var crtPemFile = GetPath(renewal, $"-crt.pem");
            var crtPem = GetPem("CERTIFICATE", certificateExport);
            _log.Information("Saving certificate to {crtDerFile}", _certificatePath);
            File.WriteAllBytes(crtDerFile, certificateExport);
            File.WriteAllText(crtPemFile, crtPem);

            // Get issuer certificate and save in DER and PEM formats
            var chain = new X509Chain();
            chain.Build(certificate);
            X509Certificate2 issuerCertificate = chain.ChainElements[1].Certificate;
            var issuerCertificateExport = issuerCertificate.Export(X509ContentType.Cert);
            var issuerPem = GetPem("CERTIFICATE", issuerCertificateExport);
            File.WriteAllBytes(GetPath(renewal, "-crt.der", "ca-"), issuerCertificateExport);
            File.WriteAllText(GetPath(renewal, "-crt.pem", "ca-"), issuerPem);

            // Generate combined files
            File.WriteAllText(GetPath(renewal, "-chain.pem", "ca-"), crtPem + issuerPem);

            // Build pfx archive
            var pfx = new bc.Pkcs.Pkcs12Store();
            var bcCertificate = ParsePem<bc.X509.X509Certificate>(crtPem);
            var bcCertificateEntry = new bc.Pkcs.X509CertificateEntry(bcCertificate);
            var bcCertificateAlias = bcCertificate.SubjectDN.ToString();
            var bcPrivateKeyEntry = new bc.Pkcs.AsymmetricKeyEntry(csrPlugin.GeneratePrivateKey());
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
                   
                tempPfx.FriendlyName = $"{renewal.FriendlyName} {DateTime.Now.ToUserString()}";
                File.WriteAllBytes(pfxFileInfo.FullName, tempPfx.Export(X509ContentType.Pfx, renewal.PfxPassword));
                pfxFileInfo.Refresh();
            }

            // Recreate X509Certificate2 with correct flags for Store/Install
            return new CertificateInfo()
            {
                Certificate = ReadForUse(pfxFileInfo, renewal.PfxPassword),
                PfxFile = pfxFileInfo
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
            // Delete cached .pfx file
            var pfx = new FileInfo(PfxFilePath(renewal));
            if (pfx.Exists)
            {
                pfx.Delete();
            }
            _log.Warning("Certificate for {target} revoked, you should renew immediately", renewal);
        }

        /// <summary>
        /// Path to the cached PFX file
        /// </summary>
        /// <param name="renewal"></param>
        /// <returns></returns>
        public string PfxFilePath(Renewal renewal)
        {
            return GetPath(renewal, "-all.pfx", "");
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
