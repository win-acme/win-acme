using ACMESharp.Crypto;
using ACMESharp.Protocol;
using bc = Org.BouncyCastle;
using PKISharp.WACS.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using PKISharp.WACS.Acme;
using PKISharp.WACS.DomainObjects;

namespace PKISharp.WACS.Services
{
    internal class CertificateService
    {
        private ILogService _log;
        private Options _options;
        private AcmeClient _client;
        private ProxyService _proxy;
        private string _configPath;
        private string _certificatePath;

        public CertificateService(IOptionsService options,
            ILogService log,
            AcmeClient client,
            ProxyService proxy,
            ISettingsService settingsService)
        {
            _log = log;
            _options = options.Options;
            _client = client;
            _configPath = settingsService.ConfigPath;
            _proxy = proxy;
            InitCertificatePath();
        }

        public string GetPath(Renewal renewal, string postfix, string prefix = "")
        {
            var fi = _certificatePath.LongFile(prefix, FileNamePart(renewal), postfix, _log);
            if (fi != null)
            {
                return fi.FullName;
            }
            else
            {
                throw new Exception("Unable to read file");
            }
        }

        private void InitCertificatePath()
        {
            _certificatePath = Properties.Settings.Default.CertificatePath;
            if (string.IsNullOrWhiteSpace(_certificatePath))
            {
                _certificatePath = Path.Combine(_configPath, "Certificates");
            }
            if (!Directory.Exists(_certificatePath))
            {
                try
                {
                    Directory.CreateDirectory(_certificatePath);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Unable to create certificate directory {_certificatePath}", _certificatePath);
                    throw;
                }
            }
            _log.Debug("Certificate folder: {_certificatePath}", _certificatePath);
        }

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

        private string GetPem(string name, byte[] content)
        {
            return GetPem(new bc.Utilities.IO.Pem.PemObject(name, content));
        }

        private T ParsePem<T>(string pem)
        {
            using (var tr = new StringReader(pem))
            {
                var pr = new bc.OpenSsl.PemReader(tr);
                return (T)pr.ReadObject();
            }
        }

        /// <summary>
        /// Request certificate from the ACME server
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        public CertificateInfo RequestCertificate(Renewal renewal, Target target, OrderDetails order)
        {
            // What are we going to get?
            var pfxPassword = Properties.Settings.Default.PFXPassword;
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
            // will only be done once per day maximum.
            if (pfxFileInfo.Exists && pfxFileInfo.LastWriteTime > DateTime.Now.AddDays(-1))
            {
                try
                {
                    var cached = new CertificateInfo()
                    {
                        Certificate = ReadForUse(pfxFileInfo, pfxPassword),
                        PfxFile = pfxFileInfo
                    };
                    var idn = new IdnMapping();
                    if (cached.SubjectName == commonName &&
                        cached.HostNames.Count == identifiers.Count() &&
                        cached.HostNames.All(h => identifiers.Contains(idn.GetAscii(h))))
                    {
                        if (_options.ForceRenewal)
                        {
                            _log.Warning("Cached certificate available but not used with --{switch}. Use 'Renew specific' or 'Renew all' in the main menu to run unscheduled renewals without hitting rate limits.", nameof(Options.ForceRenewal).ToLower());
                        }
                        else
                        {
                            _log.Warning("Using cached certificate for {friendlyName}. To force issue of a new certificate within 24 hours, delete the .pfx file from the CertificatePath or run with the --{switch} switch. Be ware that you might run into rate limits doing so.", renewal.FriendlyName, nameof(Options.ForceRenewal).ToLower());
                            return cached;
                        }

                    }
                }
                catch
                {
                    // File corrupt or invalid password?
                    _log.Warning("Unable to read from certificate cache");
                }
            }

            var serializedKeys = CryptoHelper.Rsa.GenerateKeys(RSA.Create(GetRsaKeyBits()));
            var rsa = CryptoHelper.Rsa.GenerateAlgorithm(serializedKeys);
            var csr = GetCsr(commonName, identifiers, rsa);
            var csrBytes = csr.CreateSigningRequest();
            order = _client.SubmitCsr(order, csrBytes);

            var keyParams = bc.Security.DotNetUtilities.GetRsaKeyPair(rsa.ExportParameters(true));
            File.WriteAllText(GetPath(renewal, "-key.pem"), GetPem(keyParams.Private));
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
            var bcPrivateKeyEntry = new bc.Pkcs.AsymmetricKeyEntry(keyParams.Private);
            pfx.SetCertificateEntry(bcCertificateAlias, bcCertificateEntry);
            pfx.SetKeyEntry(bcCertificateAlias, bcPrivateKeyEntry, new[] { bcCertificateEntry });

            var bcIssuer = ParsePem<bc.X509.X509Certificate>(issuerPem);
            var bcIssuerEntry = new bc.Pkcs.X509CertificateEntry(bcIssuer);
            var bcIssuerAlias = bcIssuer.SubjectDN.ToString();
            pfx.SetCertificateEntry(bcIssuerAlias, bcIssuerEntry);

            using (var pfxStream = new MemoryStream())
            {
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
                    try
                    {
                        tempPfx.PrivateKey = Convert((RSACryptoServiceProvider)tempPfx.PrivateKey);
                    }
                    catch (Exception ex)
                    {
                        // If we couldn't convert the private key that 
                        // means we're left with a pfx generated with the
                        // 'wrong' Crypto provider therefor delete it to 
                        // make sure it's retried on the next run.
                        _log.Warning("Error converting private key to Microsoft RSA SChannel Cryptographic Provider, which means it might not be usable for Exchange.");
                        _log.Verbose("{ex}", ex);
                    }
                    tempPfx.FriendlyName = FriendlyName(renewal);
                    File.WriteAllBytes(pfxFileInfo.FullName, tempPfx.Export(X509ContentType.Pfx, pfxPassword));
                    pfxFileInfo.Refresh();
                }
            }

            // Recreate X509Certificate2 with correct flags for Store/Install
            return new CertificateInfo()
            {
                Certificate = ReadForUse(pfxFileInfo, pfxPassword),
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
            if (Properties.Settings.Default.PrivateKeyExportable)
            {
                _log.Debug("Set private key exportable");
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

        private RSACryptoServiceProvider Convert(RSACryptoServiceProvider ackp)
        {
            var cspParameters = new CspParameters
            {
                KeyContainerName = Guid.NewGuid().ToString(),
                KeyNumber = 1,
                Flags = CspProviderFlags.UseMachineKeyStore,
                ProviderType = 12 // Microsoft RSA SChannel Cryptographic Provider
            };
            var rsaProvider = new RSACryptoServiceProvider(cspParameters);
            var parameters = ackp.ExportParameters(true);
            rsaProvider.ImportParameters(parameters);
            return rsaProvider;
        }

        private string FriendlyName(Renewal renewal)
        {
            return $"{renewal.FriendlyName} {DateTime.Now.ToUserString()}";
        }

        private string FileNamePart(Renewal renewal)
        {
            return renewal.Id;
        }

        public string PfxFilePath(Renewal renewal)
        {
            return GetPath(renewal, "-all.pfx", "");
        }

        /// <summary>
        /// Get the certificate signing request
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="target"></param>
        /// <param name="identifiers"></param>
        /// <param name="rsaPk"></param>
        /// <returns></returns>
        private CertificateRequest GetCsr(string commonName, List<string> identifiers, RSA rsa)
        {
            var idn = new IdnMapping();
            if (!string.IsNullOrWhiteSpace(commonName))
            {
                commonName = idn.GetAscii(commonName);
                if (!identifiers.Contains(commonName, StringComparer.InvariantCultureIgnoreCase))
                {
                    _log.Warning($"Common name {commonName} provided is invalid.");
                    commonName = null;
                }
            }
            var sanBuilder = new SubjectAlternativeNameBuilder();
            foreach (var n in identifiers)
            {
                sanBuilder.AddDnsName(n);
            }
            var finalCommonName = commonName ?? identifiers.FirstOrDefault();
            var dn = new X500DistinguishedName($"CN={finalCommonName}");
            var csr = new CertificateRequest(dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            csr.CertificateExtensions.Add(sanBuilder.Build());
            return csr;
        }

        /// <summary>
        /// Parameters to generate the key for
        /// </summary>
        /// <returns></returns>
        private int GetRsaKeyBits()
        {
            try
            {
                if (Properties.Settings.Default.RSAKeyBits >= 2048)
                {
                    _log.Debug("RSAKeyBits: {RSAKeyBits}", Properties.Settings.Default.RSAKeyBits);
                    return Properties.Settings.Default.RSAKeyBits;
                }
                else
                {
                    _log.Warning("RSA key bits less than 2048 is not secure.");
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to get RSA Key bits, error: {@ex}", ex);
            }
            return 2048;
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
