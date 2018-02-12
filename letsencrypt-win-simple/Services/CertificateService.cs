using ACMESharp;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using ACMESharp.PKI.RSA;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LetsEncrypt.ACME.Simple.Services
{
    class CertificateService
    {
        private ILogService _log;
        private Options _options;
        private LetsEncryptClient _client;
        private string _configPath;
        private string _certificatePath;

        public CertificateService(IOptionsService options, ILogService log, LetsEncryptClient client, ISettingsService settingsService)
        {
            _log = log;
            _options = options.Options;
            _client = client;
            _configPath = settingsService.ConfigPath;
            InitCertificatePath();
        }

        public string GetPath(Target target, string postfix, string prefix = "")
        {
            return Path.Combine(_certificatePath, $"{prefix}{FileNamePart(target)}{postfix}");
        }

        private void InitCertificatePath()
        {
            _certificatePath = Properties.Settings.Default.CertificatePath;
            if (string.IsNullOrWhiteSpace(_certificatePath))
            {
                _certificatePath = _configPath;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(_certificatePath);
                }
                catch (Exception ex)
                {
                    _log.Warning("Error creating the certificate directory, {_certificatePath}. Defaulting to config path. Error: {@ex}", _certificatePath, ex);
                    _certificatePath = _configPath;
                }
            }
            _log.Debug("Certificate folder: {_certificatePath}", _certificatePath);
        }

        /// <summary>
        /// Request certificate from Let's Encrypt
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        public CertificateInfo RequestCertificate(Target binding)
        {
            // What are we going to get?
            var identifiers = binding.GetHosts(false);
            var friendlyName = FriendlyName(binding);
            var pfxPassword = Properties.Settings.Default.PFXPassword;
            var pfxFileInfo = new FileInfo(PfxFilePath(binding));

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
                    if (cached.SubjectName == identifiers.First() &&
                        cached.HostNames.Count == identifiers.Count &&
                        cached.HostNames.All(h => identifiers.Contains(idn.GetAscii(h))))
                    {
                        if (_options.ForceRenewal)
                        {
                            _log.Warning("Cached certificate available but not used with --forcerenewal. Use 'Renew specific' or 'Renew all' in the main menu to run unscheduled renewals without hitting rate limits.");
                        }
                        else
                        {
                            _log.Warning("Using cached certificate for {friendlyName}. To force issue of a new certificate within 24 hours, delete the .pfx file from the CertificatePath or run with the --forcerenewal switch. Be ware that you might run into rate limits doing so.", friendlyName);
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

            using (var cp = CertificateProvider.GetProvider("BouncyCastle"))
            {
                // Generate the private key and CSR
                var rsaPkp = GetRsaKeyParameters();
                var rsaKeys = cp.GeneratePrivateKey(rsaPkp);
                var csr = GetCsr(cp, identifiers, rsaKeys);
                byte[] derRaw;
                using (var bs = new MemoryStream())
                {
                    cp.ExportCsr(csr, EncodingFormat.DER, bs);
                    derRaw = bs.ToArray();
                }
                var derB64U = JwsHelper.Base64UrlEncode(derRaw);

                // Save request parameters to disk
                using (var fs = new FileStream(GetPath(binding, "-gen-key.json"), FileMode.Create))
                    cp.SavePrivateKey(rsaKeys, fs);

                using (var fs = new FileStream(GetPath(binding, "-key.pem"), FileMode.Create))
                    cp.ExportPrivateKey(rsaKeys, EncodingFormat.PEM, fs);

                using (var fs = new FileStream(GetPath(binding, "-gen-csr.json"), FileMode.Create))
                    cp.SaveCsr(csr, fs);

                using (var fs = new FileStream(GetPath(binding, "-csr.pem"), FileMode.Create))
                    cp.ExportCsr(csr, EncodingFormat.PEM, fs);

                // Request the certificate from Let's Encrypt 
                _log.Information("Requesting certificate {friendlyName}", friendlyName);
                var certificateRequest = _client.Acme.RequestCertificate(derB64U);
                if (certificateRequest.StatusCode != HttpStatusCode.Created)
                {
                    throw new Exception($"Request status {certificateRequest.StatusCode}");
                }

                // Main certicate and issuer certificate
                Crt certificate;
                Crt issuerCertificate;

                // Certificate request was successful, save the certificate itself
                var crtDerFile = GetPath(binding, $"-crt.der");
                _log.Information("Saving certificate to {crtDerFile}", _certificatePath);
                using (var file = File.Create(crtDerFile))
                    certificateRequest.SaveCertificate(file);

                // Save certificate in PEM format too
                var crtPemFile = GetPath(binding, $"-crt.pem");
                using (FileStream source = new FileStream(crtDerFile, FileMode.Open),
                    target = new FileStream(crtPemFile, FileMode.Create))
                {
                    certificate = cp.ImportCertificate(EncodingFormat.DER, source);
                    cp.ExportCertificate(certificate, EncodingFormat.PEM, target);
                }

                // Get issuer certificate and save in DER and PEM formats
                issuerCertificate = GetIssuerCertificate(certificateRequest, cp);
                using (var target = new FileStream(GetPath(binding, "-crt.der", "ca-"), FileMode.Create))
                    cp.ExportCertificate(issuerCertificate, EncodingFormat.DER, target);

                var issuerPemFile = GetPath(binding, "-crt.pem", "ca-");
                using (var target = new FileStream(issuerPemFile, FileMode.Create))
                    cp.ExportCertificate(issuerCertificate, EncodingFormat.PEM, target);

                // Save chain in PEM format
                using (FileStream intermediate = new FileStream(issuerPemFile, FileMode.Open),
                    certificateStrean = new FileStream(crtPemFile, FileMode.Open),
                    chain = new FileStream(GetPath(binding, "-chain.pem"), FileMode.Create))
                {
                    certificateStrean.CopyTo(chain);
                    intermediate.CopyTo(chain);
                }

                // All raw data has been saved, now generate the PFX file
                using (FileStream target = new FileStream(pfxFileInfo.FullName, FileMode.Create))
                {
                    try
                    {
                        cp.ExportArchive(rsaKeys,
                            new[] { certificate, issuerCertificate },
                            ArchiveFormat.PKCS12,
                            target, 
                            pfxPassword);
                    }
                    catch (Exception ex)
                    {
                        _log.Error("Error exporting archive {@ex}", ex);
                    }
                }

                // Flags used for the internally cached certificate
                X509KeyStorageFlags internalFlags = 
                    X509KeyStorageFlags.MachineKeySet | 
                    X509KeyStorageFlags.PersistKeySet | 
                    X509KeyStorageFlags.Exportable;

                // See http://paulstovell.com/blog/x509certificate2
                try
                {
                    // Convert Private Key to different CryptoProvider
                    _log.Verbose("Converting private key...");
                    var res = new X509Certificate2(pfxFileInfo.FullName, pfxPassword, internalFlags);
                    var privateKey = (RSACryptoServiceProvider)res.PrivateKey;
                    res.PrivateKey = Convert(privateKey);
                    res.FriendlyName = friendlyName;
                    File.WriteAllBytes(pfxFileInfo.FullName, res.Export(X509ContentType.Pfx, pfxPassword));
                    pfxFileInfo.Refresh();
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

                // Recreate X509Certificate2 with correct flags for Store/Install
                return new CertificateInfo() {
                    Certificate = ReadForUse(pfxFileInfo, pfxPassword),
                    PfxFile = pfxFileInfo
                };
            }
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
            X509KeyStorageFlags externalFlags =
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
        public void RevokeCertificate(Target binding)
        {
            var fi = new FileInfo(GetPath(binding, "-crt.der"));
            if (!fi.Exists)
            {
                _log.Warning("Unable to find file {fi}", fi.FullName);
                return;
            }
            var der = File.ReadAllBytes(fi.FullName);
            var base64 = JwsHelper.Base64UrlEncode(der);
            _client.Acme.RevokeCertificate(base64);
            // Delete cached .pfx file
            var pfx = new FileInfo(PfxFilePath(binding));
            if (pfx.Exists)
            {
                pfx.Delete();
            }
            _log.Warning("Certificate for {target} revoked, you should renew immediately", binding);
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
             RSACryptoServiceProvider rsaProvider = new RSACryptoServiceProvider(cspParameters);
             RSAParameters parameters = ackp.ExportParameters(true);
             rsaProvider.ImportParameters(parameters);
             return rsaProvider;
        }

        private string FriendlyName(Target target)
        {
            return $"{target.Host} {DateTime.Now.ToString(Properties.Settings.Default.FileDateFormat)}";
        }

        private string FileNamePart(Target target)
        {
            return target.Host.CleanFileName();
        }

        public string PfxFilePath(Target target)
        {
            return PfxFilePath(FileNamePart(target));
        }

        public string PfxFilePath(string target)
        {
            return Path.Combine(_certificatePath, $"{target}-all.pfx");
        }

        /// <summary>
        /// Get the certificate signing request
        /// </summary>
        /// <param name="cp"></param>
        /// <param name="target"></param>
        /// <param name="identifiers"></param>
        /// <param name="rsaPk"></param>
        /// <returns></returns>
        private Csr GetCsr(CertificateProvider cp, List<string> identifiers, PrivateKey rsaPk)
        {
            var csr = cp.GenerateCsr(new CsrParams
            {
                Details = new CsrDetails()
                {
                    CommonName = identifiers.FirstOrDefault(),
                    AlternativeNames = identifiers
                }
            }, rsaPk, Crt.MessageDigest.SHA256);
            return csr;
        }

        /// <summary>
        /// Parameters to generate the key for
        /// </summary>
        /// <returns></returns>
        private RsaPrivateKeyParams GetRsaKeyParameters()
        {
            var rsaPkp = new RsaPrivateKeyParams();
            try
            {
                if (Properties.Settings.Default.RSAKeyBits >= 1024)
                {
                    rsaPkp.NumBits = Properties.Settings.Default.RSAKeyBits;
                    _log.Debug("RSAKeyBits: {RSAKeyBits}", Properties.Settings.Default.RSAKeyBits);
                }
                else
                {
                    _log.Warning("RSA Key Bits less than 1024 is not secure. Letting ACMESharp default key bits. http://openssl.org/docs/manmaster/crypto/RSA_generate_key_ex.html");
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to set RSA Key Bits, Letting ACMESharp default key bits, Error: {@ex}", ex);
            }
            return rsaPkp;
        }

        /// <summary>
        /// Get the issuer certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="cp"></param>
        /// <returns></returns>
        private Crt GetIssuerCertificate(CertificateRequest certificate, CertificateProvider cp)
        {
            var linksEnum = certificate.Links;
            if (linksEnum != null)
            {
                var links = new LinkCollection(linksEnum);
                var upLink = links.GetFirstOrDefault("up");
                if (upLink != null)
                {
                    using (var web = new WebClient())
                    using (var stream = web.OpenRead(new Uri(new Uri(_options.BaseUri), upLink.Uri)))
                    {
                        return cp.ImportCertificate(EncodingFormat.DER, stream);
                    }
                }
            }
            return null;
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
