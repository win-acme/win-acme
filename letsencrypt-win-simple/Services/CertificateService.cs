using ACMESharp;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using ACMESharp.PKI.RSA;
using LetsEncrypt.ACME.Simple.Clients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Services
{
    class CertificateService
    {
        private ILogService _log;
        private Options _options;
        private LetsEncryptClient _client;
        private string _configPath;
        private string _certificatePath;

        public CertificateService(Options options, ILogService log, LetsEncryptClient client, string configPath)
        {
            _log = log;
            _options = options;
            _client = client;
            _configPath = configPath;
            InitCertificatePath();
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
            var fileName = FileNamePart(binding);
            var friendlyName = FriendlyName(binding);

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
                var keyGenFile = Path.Combine(_certificatePath, $"{fileName}-gen-key.json");
                using (var fs = new FileStream(keyGenFile, FileMode.Create))
                    cp.SavePrivateKey(rsaKeys, fs);

                var keyPemFile = Path.Combine(_certificatePath, $"{fileName}-key.pem");
                using (var fs = new FileStream(keyPemFile, FileMode.Create))
                    cp.ExportPrivateKey(rsaKeys, EncodingFormat.PEM, fs);

                var csrGenFile = Path.Combine(_certificatePath, $"{fileName}-gen-csr.json");
                using (var fs = new FileStream(csrGenFile, FileMode.Create))
                    cp.SaveCsr(csr, fs);

                var csrPemFile = Path.Combine(_certificatePath, $"{fileName}-csr.pem");
                using (var fs = new FileStream(csrPemFile, FileMode.Create))
                    cp.ExportCsr(csr, EncodingFormat.PEM, fs);

                // Request the certificate from Let's Encrypt 
                _log.Information("Requesting certificate {friendlyName}", friendlyName);
                var certificateRequest = _client.Acme.RequestCertificate(derB64U);
                if (certificateRequest.StatusCode != HttpStatusCode.Created)
                {
                    var ex = new Exception($"Request status {certificateRequest.StatusCode}");
                    _log.Error(ex, "Certificate request failed");
                    throw ex;
                }

                // Main certicate and issuer certificate
                Crt certificate;
                Crt issuerCertificate;

                // Certificate request was successful, save the certificate itself
                var crtDerFile = Path.Combine(_certificatePath, $"{fileName}-crt.der");
                _log.Information("Saving certificate to {crtDerFile}", _certificatePath);
                using (var file = File.Create(crtDerFile))
                    certificateRequest.SaveCertificate(file);

                // Save certificate in PEM format too
                var crtPemFile = Path.Combine(_certificatePath, $"{fileName}-crt.pem");
                using (FileStream source = new FileStream(crtDerFile, FileMode.Open),
                    target = new FileStream(crtPemFile, FileMode.Create))
                {
                    certificate = cp.ImportCertificate(EncodingFormat.DER, source);
                    cp.ExportCertificate(certificate, EncodingFormat.PEM, target);
                }

                // Get issuer certificate and save in DER and PEM formats
                issuerCertificate = GetIssuerCertificate(certificateRequest, cp);
                var issuerDerFile = Path.Combine(_certificatePath, $"ca-{fileName}-crt.der");
                using (var target = new FileStream(issuerDerFile, FileMode.Create))
                    cp.ExportCertificate(issuerCertificate, EncodingFormat.DER, target);

                var issuerPemFile = Path.Combine(_certificatePath, $"ca-{fileName}-crt.pem");
                using (var target = new FileStream(issuerPemFile, FileMode.Create))
                    cp.ExportCertificate(issuerCertificate, EncodingFormat.PEM, target);

                // Save chain in PEM format
                var chainPemFile = Path.Combine(_certificatePath, $"{fileName}-chain.pem");
                using (FileStream intermediate = new FileStream(issuerPemFile, FileMode.Open),
                    certificateStrean = new FileStream(crtPemFile, FileMode.Open),
                    chain = new FileStream(chainPemFile, FileMode.Create))
                {
                    certificateStrean.CopyTo(chain);
                    intermediate.CopyTo(chain);
                }

                // All raw data has been saved, now generate the PFX file
                var pfxFile = PfxFilePath(binding);
                var pfxPassword = Properties.Settings.Default.PFXPassword;
                using (FileStream target = new FileStream(pfxFile, FileMode.Create))
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

                X509KeyStorageFlags flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
                if (Properties.Settings.Default.PrivateKeyExportable)
                {
                    _log.Debug("Set private key exportable");
                    flags |= X509KeyStorageFlags.Exportable;
                }

                // See http://paulstovell.com/blog/x509certificate2
                var res = new X509Certificate2(pfxFile, pfxPassword, flags);
                var privateKey = (RSACryptoServiceProvider)res.PrivateKey;
                res.PrivateKey = Convert(privateKey);
                res.FriendlyName = friendlyName;
                File.WriteAllBytes(pfxFile, res.Export(X509ContentType.Pfx, pfxPassword));
                return new CertificateInfo() { Certificate = res, PfxFile = new FileInfo(pfxFile) };
            }
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
            var identifiers = target.GetHosts(false);
            return identifiers.First();
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
        public static Func<X509Certificate2, bool> FriendlyNameFilter(string friendlyName)
        {
            return new Func<X509Certificate2, bool>(x => {
                return (x.Issuer.Contains("LE Intermediate") || x.Issuer.Contains("Let's Encrypt")) &&
                       x.FriendlyName.ToLower().StartsWith(friendlyName.ToLower()); 
                }
            );
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
