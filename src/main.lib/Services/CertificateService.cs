using ACMESharp;
using ACMESharp.Protocol;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Bc = Org.BouncyCastle;

namespace PKISharp.WACS.Services
{
    internal class CertificateService : ICertificateService
    {
        private readonly IInputService _inputService;
        private readonly ILogService _log;
        private readonly ICacheService _cacheService;
        private readonly AcmeClient _client;
        private readonly PemService _pemService;
        private readonly CertificatePicker _picker;
        private readonly ISettingsService _settings;

        public CertificateService(
            ILogService log,
            ISettingsService settings,
            AcmeClient client,
            PemService pemService,
            IInputService inputService,
            ICacheService cacheService,
            CertificatePicker picker)
        {
            _log = log;
            _client = client;
            _settings = settings;
            _pemService = pemService;
            _cacheService = cacheService;
            _inputService = inputService;
            _picker = picker;
        }

        /// <summary>
        /// Request certificate from the ACME server
        /// </summary>
        /// <param name="csrPlugin">PluginBackend used to generate CSR if it has not been provided in the target</param>
        /// <param name="runLevel"></param>
        /// <param name="renewal"></param>
        /// <param name="target"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public async Task<ICertificateInfo> RequestCertificate(ICsrPlugin? csrPlugin, Order order)
        {
            if (order.Details == null)
            {
                throw new InvalidOperationException("No order details found");
            }

            // What are we going to get?
            var friendlyName = order.FriendlyNameIntermediate;
            if (_settings.Security.FriendlyNameDateTimeStamp != false)
            {
                friendlyName = $"{friendlyName} @ {_inputService.FormatDate(DateTime.Now)}";
            }

            // Generate the CSR here, because we want to save it 
            // in the certificate cache folder even though we might
            // not need to submit it to the server in case of a 
            // cached order
            order.Target.CsrBytes = order.Target.UserCsrBytes;
            if (order.Target.CsrBytes == null)
            {
                if (csrPlugin == null)
                {
                    throw new InvalidOperationException("Missing CsrPlugin");
                }
                var csr = await csrPlugin.GenerateCsr(order.Target, order.KeyPath);
                var keySet = await csrPlugin.GetKeys();
                order.Target.CsrBytes = csr.GetDerEncoded();
                order.Target.PrivateKey = keySet.Private;
            }

            if (order.Target.CsrBytes == null)
            {
                throw new InvalidOperationException("No CsrBytes found");
            }
            await _cacheService.StoreCsr(order, _pemService.GetPem("CERTIFICATE REQUEST", order.Target.CsrBytes.ToArray()));

            // Check order status
            if (order.Details.Payload.Status != AcmeClient.OrderValid)
            {
                // Finish the order by sending the CSR to 
                // the server, which can then generate the
                // certificate.
                _log.Verbose("Submitting CSR");
                order.Details = await _client.SubmitCsr(order.Details, order.Target.CsrBytes.ToArray());
                if (order.Details.Payload.Status != AcmeClient.OrderValid)
                {
                    _log.Error("Unexpected order status {status}", order.Details.Payload.Status);
                    throw new Exception($"Unable to complete order");
                }
            }

            // Download the certificate from the server
            _log.Information("Downloading certificate {friendlyName}", order.FriendlyNameIntermediate);
            var selected = await DownloadCertificate(order.Details, friendlyName, order.Target.PrivateKey);

            // Update LastFriendlyName so that the user sees
            // the most recently issued friendlyName in
            // the WACS GUI
            order.Renewal.LastFriendlyName = order.FriendlyNameBase;

            // Optionally store the certificate in cache
            // for future reuse. Will either return the original
            // in-memory certificate or a new cached instance with
            // pointer to a disk file (which may be used by some
            // installation scripts)
            var info = await _cacheService.StorePfx(order, selected);
            return info;
        }

        /// <summary>
        /// Download all potential certificates and pick the right one
        /// </summary>
        /// <param name="order"></param>
        /// <param name="friendlyName"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<CertificateOption> DownloadCertificate(AcmeOrderDetails order, string friendlyName, AsymmetricKeyParameter? pk)
        {
            AcmeCertificate? certInfo;
            try
            {
                certInfo = await _client.GetCertificate(order);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to get certificate", ex);
            }
            if (certInfo == default || certInfo.Certificate == null)
            {
                throw new Exception($"Unable to get certificate");
            }
            var alternatives = new List<CertificateOption>
            {
                new(
                    withPrivateKey: ParseCertificate(certInfo.Certificate, friendlyName, pk),
                    withoutPrivateKey: ParseCertificate(certInfo.Certificate, friendlyName)
                )
            };
            if (certInfo.Links != null)
            {
                foreach (var alt in certInfo.Links["alternate"])
                {
                    try
                    {
                        var altCertRaw = await _client.GetCertificate(alt);
                        var altCert = new CertificateOption(
                            withPrivateKey: ParseCertificate(altCertRaw, friendlyName, pk),
                            withoutPrivateKey: ParseCertificate(altCertRaw, friendlyName)
                        );
                        alternatives.Add(altCert);
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("Unable to get alternate certificate: {ex}", ex.Message);
                    }
                }
            }
            return _picker.Select(alternatives);
        }
      
        /// <summary>
        /// Parse bytes to a usable certificate
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="friendlyName"></param>
        /// <param name="pk"></param>
        /// <returns></returns>
        private CertificateInfo ParseCertificate(byte[] bytes, string friendlyName, AsymmetricKeyParameter? pk = null, int attempt = 0)
        {
            // Build pfx archive including any intermediates provided
            if (attempt == 0)
            {
                _log.Verbose("Parsing certificate from {bytes} bytes received", bytes.Length);
            } 
            else
            {
                _log.Verbose("Parsing certificate from {bytes} bytes received (attempt {n})", bytes.Length, attempt+1);
            }
            var text = Encoding.UTF8.GetString(bytes);
            var pfxBuilder = new Bc.Pkcs.Pkcs12StoreBuilder();
            if (attempt == 1)
            {
                // On second try, use different algorithms, because that 
                // might be the reason for "Bad Data" exception
                // As found in https://github.com/bcgit/bc-csharp/discussions/372
                pfxBuilder.SetKeyAlgorithm(
                    NistObjectIdentifiers.IdAes256Cbc, 
                    PkcsObjectIdentifiers.IdHmacWithSha256);
            }
            pfxBuilder.SetUseDerEncoding(true);
            var pfx = pfxBuilder.Build();
            var startIndex = 0;
            const string startString = "-----BEGIN CERTIFICATE-----";
            const string endString = "-----END CERTIFICATE-----";
            while (true)
            {
                startIndex = text.IndexOf(startString, startIndex, StringComparison.Ordinal);
                if (startIndex < 0)
                {
                    break;
                }
                var endIndex = text.IndexOf(endString, startIndex, StringComparison.Ordinal);
                if (endIndex < 0)
                {
                    break;
                }
                endIndex += endString.Length;
                var pem = text[startIndex..endIndex];
                _log.Verbose("Parsing PEM data at range {startIndex}..{endIndex}", startIndex, endIndex);
                var bcCertificate = _pemService.ParsePem<Bc.X509.X509Certificate>(pem);
                if (bcCertificate != null)
                {
                    var bcCertificateEntry = new Bc.Pkcs.X509CertificateEntry(bcCertificate);
                    _log.Verbose("Certificate {name} parsed", bcCertificateEntry.Certificate.SubjectDN);
                    var bcCertificateAlias = startIndex == 0 ?
                        friendlyName :
                        bcCertificate.SubjectDN.ToString();
                    pfx.SetCertificateEntry(bcCertificateAlias, bcCertificateEntry);

                    // Assume that the first certificate in the reponse is the main one
                    // so we associate the private key with that one. Other certificates
                    // are intermediates
                    if (pfx.Count == 1 && pk != null)
                    {
                        _log.Verbose($"Associating private key");
                        var bcPrivateKeyEntry = new Bc.Pkcs.AsymmetricKeyEntry(pk);
                        pfx.SetKeyEntry(bcCertificateAlias, bcPrivateKeyEntry, new[] { bcCertificateEntry });
                    }
                }
                else
                {
                    _log.Warning("PEM data could not be parsed as X509Certificate", startIndex, endIndex);
                }

                // This should never happen, but is a sanity check
                // not to get stuck in an infinite loop
                if (endIndex <= startIndex)
                {
                    _log.Error("Infinite loop detected, aborting");
                    break;
                }
                startIndex = endIndex;
            }

            try
            {
                var tempPfx = new X509Certificate2Collection();
                var tempPassword = PasswordGenerator.Generate();
                var pfxStream = new MemoryStream();
                pfx.Save(pfxStream, tempPassword.ToCharArray(), new Bc.Security.SecureRandom());
                pfxStream.Position = 0;
                using var pfxStreamReader = new BinaryReader(pfxStream);
                tempPfx = new X509Certificate2Collection();
                tempPfx.Import(
                    pfxStreamReader.ReadBytes((int)pfxStream.Length),
                    tempPassword,
                    X509KeyStorageFlags.EphemeralKeySet |
                    X509KeyStorageFlags.Exportable);
                return new CertificateInfo(tempPfx);
            } 
            catch (CryptographicException cex)
            {
                if (attempt < 1)
                {
                    // Something wrong with the BC PFX? Try again...
                    _log.Warning("Internal error, retrying with different parameters...");
                    return ParseCertificate(bytes, friendlyName, pk, attempt + 1);
                }
                else
                {
                    _log.Error(cex, "Internal error parsing certificate");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Internal error parsing certificate");
                throw;
            }

        }
    }
}