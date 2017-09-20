using ACMESharp;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using ACMESharp.PKI.RSA;
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
        private LogService _log;
        private string _certificateStore = "WebHosting";
        private string _configPath;
        private string _certificatePath;
        private Options _options;
        private AcmeClient _client;

        public CertificateService(Options options, LogService log, AcmeClient client, string configPath)
        {
            _log = log;
            _options = options;
            _client = client;
            _configPath = configPath;
            ParseCertificateStore();
            InitCertificatePath();
        }

        private void InitCertificatePath()
        {
            _certificatePath = Properties.Settings.Default.CertificatePath;
            if (string.IsNullOrWhiteSpace(_certificatePath)) {
                _certificatePath = _configPath;
            } else { 
                try {
                    Directory.CreateDirectory(_certificatePath);
                } catch (Exception ex) {
                    _log.Warning("Error creating the certificate directory, {_certificatePath}. Defaulting to config path. Error: {@ex}", _certificatePath, ex);
                    _certificatePath = _configPath;
                }
            }
            _log.Debug("Certificate folder: {_certificatePath}", _certificatePath);
        }

        private void ParseCertificateStore()
        {
            try
            {
                _certificateStore = Properties.Settings.Default.CertificateStore;
                _log.Debug("Certificate store: {_certificateStore}", _certificateStore);
            }
            catch (Exception ex)
            {
                _log.Warning("Error reading CertificateStore from config, defaulting to {_certificateStore} Error: {@ex}", _certificateStore, ex);
            }
        }

        public void InstallCertificate(Target binding, string pfxFilename, out X509Store store, out X509Certificate2 certificate)
        {
            X509Store imStore = null;
            //X509Store rootStore = null;
            try
            {
                store = new X509Store(_certificateStore, StoreLocation.LocalMachine);
                imStore = new X509Store(StoreName.CertificateAuthority, StoreLocation.LocalMachine);
                //rootStore = new X509Store(StoreName.AuthRoot, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                imStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
                //rootStore.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                _log.Error("Error encountered while opening certificate store. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }

            _log.Debug("Opened Certificate Store {Name}", store.Name);
            certificate = null;
            try
            {
                X509KeyStorageFlags flags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;
                if (Properties.Settings.Default.PrivateKeyExportable)
                {
                    _log.Debug("Set private key exportable");
                    flags |= X509KeyStorageFlags.Exportable;
                }

                // See http://paulstovell.com/blog/x509certificate2
                certificate = new X509Certificate2(pfxFilename, Properties.Settings.Default.PFXPassword, flags);
                certificate.FriendlyName = $"{binding.Host} {DateTime.Now.ToString(Properties.Settings.Default.FileDateFormat)}";
                _log.Debug("Adding certificate {FriendlyName} to store", certificate.FriendlyName);
                X509Chain chain = new X509Chain();
                chain.Build(certificate);
                foreach (var chainElement in chain.ChainElements)
                {
                    var cert = chainElement.Certificate;
                    if (cert.HasPrivateKey)
                    {
                        store.Add(cert);
                    }
                    else
                    {
                        imStore.Add(cert);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error saving certificate {@ex}", ex);
            }
            _log.Debug("Closing certificate store");
            store.Close();
            imStore.Close();
            //rootStore.Close();
        }

        public void UninstallCertificate(string host, out X509Store store, X509Certificate2 certificate)
        {
            try
            {
                store = new X509Store(_certificateStore, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (CryptographicException)
            {
                store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);
            }
            catch (Exception ex)
            {
                _log.Error("Error encountered while opening certificate store. Error: {@ex}", ex);
                throw new Exception(ex.Message);
            }

            _log.Debug("Opened certificate store {Name}", store.Name);
            try
            {
                X509Certificate2Collection col = store.Certificates;
                foreach (var cert in col)
                {
                    if ((cert.Issuer.Contains("LE Intermediate") || cert.Issuer.Contains("Let's Encrypt")) && // Only delete Let's Encrypt certificates
                        cert.FriendlyName.StartsWith(host + " ") && // match by friendly name
                        cert.Thumbprint != certificate.Thumbprint) // don't delete the most recently installed one
                    {
                        _log.Information("Removing certificate {@cert}", cert.FriendlyName);
                        store.Remove(cert);
                    }
                }
                _log.Debug("Closing certificate store");
            }
            catch (Exception ex)
            {
                _log.Error("Error removing certificate {@ex}", ex);
            }
            store.Close();
        }

        public string GetCertificate(Target binding)
        {

            List<string> identifiers = binding.GetHosts(false);
            var identifier = identifiers.First();

            var cp = CertificateProvider.GetProvider("BouncyCastle");
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

            var rsaKeys = cp.GeneratePrivateKey(rsaPkp);
            var csrDetails = new CsrDetails()
            {
                CommonName = identifiers.FirstOrDefault(),
                AlternativeNames = identifiers
            };

            var csrParams = new CsrParams
            {
                Details = csrDetails
            };
            var csr = cp.GenerateCsr(csrParams, rsaKeys, Crt.MessageDigest.SHA256);

            byte[] derRaw;
            using (var bs = new MemoryStream())
            {
                cp.ExportCsr(csr, EncodingFormat.DER, bs);
                derRaw = bs.ToArray();
            }
            var derB64U = JwsHelper.Base64UrlEncode(derRaw);

            _log.Information($"Requesting certificate: {identifier}");
            var certRequ = _client.RequestCertificate(derB64U);

            //Log.Debug("certRequ {@certRequ}", certRequ);
            _log.Debug("Request Status: {statusCode}", certRequ.StatusCode);

            if (certRequ.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var keyGenFile = Path.Combine(_certificatePath, $"{identifier}-gen-key.json");
                var keyPemFile = Path.Combine(_certificatePath, $"{identifier}-key.pem");
                var csrGenFile = Path.Combine(_certificatePath, $"{identifier}-gen-csr.json");
                var csrPemFile = Path.Combine(_certificatePath, $"{identifier}-csr.pem");
                var crtDerFile = Path.Combine(_certificatePath, $"{identifier}-crt.der");
                var crtPemFile = Path.Combine(_certificatePath, $"{identifier}-crt.pem");
                var chainPemFile = Path.Combine(_certificatePath, $"{identifier}-chain.pem");
                string crtPfxFile = null;
                if (!_options.CentralSsl)
                {
                    crtPfxFile = Path.Combine(_certificatePath, $"{identifier}-all.pfx");
                }
                else
                {
                    crtPfxFile = Path.Combine(_options.CentralSslStore, $"{identifier}.pfx");
                }

                using (var fs = new FileStream(keyGenFile, FileMode.Create))
                    cp.SavePrivateKey(rsaKeys, fs);
                using (var fs = new FileStream(keyPemFile, FileMode.Create))
                    cp.ExportPrivateKey(rsaKeys, EncodingFormat.PEM, fs);
                using (var fs = new FileStream(csrGenFile, FileMode.Create))
                    cp.SaveCsr(csr, fs);
                using (var fs = new FileStream(csrPemFile, FileMode.Create))
                    cp.ExportCsr(csr, EncodingFormat.PEM, fs);

                _log.Information("Saving certificate to {crtDerFile}", crtDerFile);
                using (var file = File.Create(crtDerFile))
                    certRequ.SaveCertificate(file);

                Crt crt;
                using (FileStream source = new FileStream(crtDerFile, FileMode.Open),
                    target = new FileStream(crtPemFile, FileMode.Create))
                {
                    crt = cp.ImportCertificate(EncodingFormat.DER, source);
                    cp.ExportCertificate(crt, EncodingFormat.PEM, target);
                }

                // To generate a PKCS#12 (.PFX) file, we need the issuer's public certificate
                var isuPemFile = GetIssuerCertificate(certRequ, cp);

                using (FileStream intermediate = new FileStream(isuPemFile, FileMode.Open),
                    certificate = new FileStream(crtPemFile, FileMode.Open),
                    chain = new FileStream(chainPemFile, FileMode.Create))
                {
                    certificate.CopyTo(chain);
                    intermediate.CopyTo(chain);
                }

                _log.Debug($"CentralSsl {_options.CentralSsl} - San {binding.HostIsDns == true}");

                //Central SSL and San need to save the cert for each hostname
                if (_options.CentralSsl && binding.HostIsDns == true)
                {
                    foreach (var host in identifiers)
                    {
                        _log.Debug($"Host: {host}");
                        crtPfxFile = Path.Combine(_options.CentralSslStore, $"{host}.pfx");

                        _log.Information("Saving certificate to {crtPfxFile}", crtPfxFile);
                        using (FileStream source = new FileStream(isuPemFile, FileMode.Open),
                            target = new FileStream(crtPfxFile, FileMode.Create))
                        {
                            try
                            {
                                var isuCrt = cp.ImportCertificate(EncodingFormat.PEM, source);
                                cp.ExportArchive(rsaKeys, new[] { crt, isuCrt }, ArchiveFormat.PKCS12, target,
                                    Properties.Settings.Default.PFXPassword);
                            }
                            catch (Exception ex)
                            {
                                _log.Error("Error exporting archive {@ex}", ex);
                            }
                        }
                    }
                }
                else
                {
                    _log.Information("Saving certificate to {crtPfxFile}", crtPfxFile);
                    using (FileStream source = new FileStream(isuPemFile, FileMode.Open),
                        target = new FileStream(crtPfxFile, FileMode.Create))
                    {
                        try
                        {
                            var isuCrt = cp.ImportCertificate(EncodingFormat.PEM, source);
                            cp.ExportArchive(rsaKeys, new[] { crt, isuCrt }, ArchiveFormat.PKCS12, target,
                                Properties.Settings.Default.PFXPassword);
                        }
                        catch (Exception ex)
                        {
                            _log.Error("Error exporting archive {@ex}", ex);
                        }
                    }
                }

                cp.Dispose();

                return crtPfxFile;
            }
            _log.Error("Request status = {StatusCode}", certRequ.StatusCode);
            throw new Exception($"Request status = {certRequ.StatusCode}");
        }

        private string GetIssuerCertificate(CertificateRequest certificate, CertificateProvider cp)
        {
            var linksEnum = certificate.Links;
            if (linksEnum != null)
            {
                var links = new LinkCollection(linksEnum);
                var upLink = links.GetFirstOrDefault("up");
                if (upLink != null)
                {
                    var temporaryFileName = Path.Combine(_certificatePath, $"crt.tmp");
                    try
                    {
                        using (var web = new WebClient())
                        {
                            var uri = new Uri(new Uri(_options.BaseUri), upLink.Uri);
                            web.DownloadFile(uri, temporaryFileName);
                        }

                        var cacert = new X509Certificate2(temporaryFileName);
                        var sernum = cacert.GetSerialNumberString();

                        var cacertDerFile = Path.Combine(_certificatePath, $"ca-{sernum}-crt.der");
                        var cacertPemFile = Path.Combine(_certificatePath, $"ca-{sernum}-crt.pem");

                        if (!File.Exists(cacertDerFile))
                            File.Copy(temporaryFileName, cacertDerFile, true);

                        _log.Information("Saving issuer certificate to {cacertPemFile}", cacertPemFile);
                        if (!File.Exists(cacertPemFile))
                            using (FileStream source = new FileStream(cacertDerFile, FileMode.Open),
                                target = new FileStream(cacertPemFile, FileMode.Create))
                            {
                                var caCrt = cp.ImportCertificate(EncodingFormat.DER, source);
                                cp.ExportCertificate(caCrt, EncodingFormat.PEM, target);
                            }

                        return cacertPemFile;
                    }
                    finally
                    {
                        if (File.Exists(temporaryFileName))
                            File.Delete(temporaryFileName);
                    }
                }
            }

            return null;
        }

    }
}
