using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using ACMESharp;
using ACMESharp.ACME;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using LetsEncryptWinSimple.Core.Configuration;
using LetsEncryptWinSimple.Core.Interfaces;
using Serilog;

namespace LetsEncryptWinSimple.Core.Services
{
    public class LetsEncryptService : ILetsEncryptService
    {
        protected IOptions Options;
        protected IConsoleService ConsoleService;
        public LetsEncryptService(IOptions options, IConsoleService consoleService)
        {
            Options = options;
            ConsoleService = consoleService;
        }

        public AuthorizationState Authorize(Target target)
        {
            var dnsIdentifiers = new List<string>();
            if (!Options.San)
            {
                dnsIdentifiers.Add(target.Host);
            }

            if (target.AlternativeNames != null)
            {
                dnsIdentifiers.AddRange(target.AlternativeNames);
            }

            var authStatus = new List<AuthorizationState>();

            foreach (var dnsIdentifier in dnsIdentifiers)
            {
                var webRootPath = target.WebRootPath;

                Log.Information("Authorizing Identifier {dnsIdentifier} Using Challenge Type {CHALLENGE_TYPE_HTTP}",
                    dnsIdentifier, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                var authzState = Options.AcmeClient.AuthorizeIdentifier(dnsIdentifier);
                var challenge = Options.AcmeClient.DecodeChallenge(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                var httpChallenge = challenge.Challenge as HttpChallenge;
                
                if (httpChallenge == null)
                    continue;

                // We need to strip off any leading '/' in the path
                var filePath = httpChallenge.FilePath;
                if (filePath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                    filePath = filePath.Substring(1);
                var answerPath = Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath, filePath));

                var plugin = Options.Plugins[target.PluginName];
                plugin.CreateAuthorizationFile(answerPath, httpChallenge.FileContent);
                plugin.BeforeAuthorize(target, answerPath, httpChallenge.Token);

                var answerUri = new Uri(httpChallenge.FileUrl);

                if (Options.Warmup)
                {
                    Log.Information("Waiting for site to warmup...");
                    WarmupSite(answerUri);
                }

                Log.Information("Answer should now be browsable at {answerUri}", answerUri);

                try
                {
                    Log.Information("Submitting answer");
                    authzState.Challenges = new[] { challenge };
                    Options.AcmeClient.SubmitChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP, true);

                    // have to loop to wait for server to stop being pending.
                    // TODO: put timeout/retry limit in this loop
                    while (authzState.Status == "pending")
                    {
                        Log.Information("Refreshing authorization");
                        Thread.Sleep(4000); // this has to be here to give ACME server a chance to think
                        var newAuthzState = Options.AcmeClient.RefreshIdentifierAuthorization(authzState);
                        if (newAuthzState.Status != "pending")
                            authzState = newAuthzState;
                    }

                    Log.Information("Authorization Result: {Status}", authzState.Status);
                    if (authzState.Status == "invalid")
                    {
                        Log.Error("Authorization Failed {Status}", authzState.Status);
                        Log.Debug("Full Error Details {@authzState}", authzState);
                        Log.Error("The ACME server was probably unable to reach {answerUri}", answerUri);
                        ConsoleService.WriteError($"The ACME server was probably unable to reach {answerUri}\nCheck in a browser to see if the answer file is being served correctly.");
                        plugin.OnAuthorizeFail(target);
                    }

                    authStatus.Add(authzState);
                }
                finally
                {
                    if (authzState.Status == "valid")
                        plugin.DeleteAuthorization(answerPath, httpChallenge.Token, webRootPath, filePath);
                }
            }

            foreach (var authState in authStatus)
                if (authState.Status != "valid")
                    return authState;

            return new AuthorizationState { Status = "valid" };
        }

        public void WarmupSite(Uri uri)
        {
            var request = WebRequest.Create(uri);

            try
            {
                using (request.GetResponse()) { }
            }
            catch (Exception ex)
            {
                Log.Error("Error warming up site: {@ex}", ex);
            }
        }

        public string GetCertificate(Target binding)
        {
            var dnsIdentifier = binding.Host;
            var sanList = binding.AlternativeNames;
            var allDnsIdentifiers = new List<string>();

            if (!Options.San)
                allDnsIdentifiers.Add(binding.Host);
            
            if (binding.AlternativeNames != null)
                allDnsIdentifiers.AddRange(binding.AlternativeNames);
            
            var cp = CertificateProvider.GetProvider();
            var rsaPkp = new RsaPrivateKeyParams();
            try
            {
                if (Properties.Settings.Default.RSAKeyBits >= 1024)
                {
                    rsaPkp.NumBits = Properties.Settings.Default.RSAKeyBits;
                    Log.Debug("RSAKeyBits: {RSAKeyBits}", Properties.Settings.Default.RSAKeyBits);
                }
                else
                {
                    Log.Warning(
                        "RSA Key Bits less than 1024 is not secure. Letting ACMESharp default key bits. http://openssl.org/docs/manmaster/crypto/RSA_generate_key_ex.html");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Unable to set RSA Key Bits, Letting ACMESharp default key bits, Error: {@ex}", ex);
            }

            var rsaKeys = cp.GeneratePrivateKey(rsaPkp);
            var csrDetails = new CsrDetails
            {
                CommonName = allDnsIdentifiers[0],
            };

            if (sanList != null && sanList.Count > 0)
                csrDetails.AlternativeNames = sanList;

            var csrParams = new CsrParams { Details = csrDetails };
            var csr = cp.GenerateCsr(csrParams, rsaKeys, Crt.MessageDigest.SHA256);

            byte[] derRaw;
            using (var bs = new MemoryStream())
            {
                cp.ExportCsr(csr, EncodingFormat.DER, bs);
                derRaw = bs.ToArray();
            }
            var derB64U = JwsHelper.Base64UrlEncode(derRaw);

            Log.Information("Requesting Certificate");
            var certificateRequest = Options.AcmeClient.RequestCertificate(derB64U);

            Log.Debug("certRequ {@certRequ}", certificateRequest);
            Log.Information("Request Status: {StatusCode}", certificateRequest.StatusCode);

            if (certificateRequest.StatusCode == HttpStatusCode.Created)
            {
                var keyGenFile = Path.Combine(Options.CertOutPath, $"{dnsIdentifier}-gen-key.json");
                var keyPemFile = Path.Combine(Options.CertOutPath, $"{dnsIdentifier}-key.pem");
                var csrGenFile = Path.Combine(Options.CertOutPath, $"{dnsIdentifier}-gen-csr.json");
                var csrPemFile = Path.Combine(Options.CertOutPath, $"{dnsIdentifier}-csr.pem");
                var crtDerFile = Path.Combine(Options.CertOutPath, $"{dnsIdentifier}-crt.der");
                var crtPemFile = Path.Combine(Options.CertOutPath, $"{dnsIdentifier}-crt.pem");
                var chainPemFile = Path.Combine(Options.CertOutPath, $"{dnsIdentifier}-chain.pem");

                string crtPfxFile;
                if (!Options.CentralSsl)
                    crtPfxFile = Path.Combine(Options.CertOutPath, $"{dnsIdentifier}-all.pfx");
                else
                    crtPfxFile = Path.Combine(Options.CentralSslStore, $"{dnsIdentifier}.pfx");

                using (var fs = new FileStream(keyGenFile, FileMode.Create))
                    cp.SavePrivateKey(rsaKeys, fs);
                using (var fs = new FileStream(keyPemFile, FileMode.Create))
                    cp.ExportPrivateKey(rsaKeys, EncodingFormat.PEM, fs);
                using (var fs = new FileStream(csrGenFile, FileMode.Create))
                    cp.SaveCsr(csr, fs);
                using (var fs = new FileStream(csrPemFile, FileMode.Create))
                    cp.ExportCsr(csr, EncodingFormat.PEM, fs);

                Log.Information("Saving Certificate to {crtDerFile}", crtDerFile);
                using (var file = File.Create(crtDerFile))
                    certificateRequest.SaveCertificate(file);

                Crt crt;
                using (FileStream source = new FileStream(crtDerFile, FileMode.Open),
                    target = new FileStream(crtPemFile, FileMode.Create))
                {
                    crt = cp.ImportCertificate(EncodingFormat.DER, source);
                    cp.ExportCertificate(crt, EncodingFormat.PEM, target);
                }

                // To generate a PKCS#12 (.PFX) file, we need the issuer's public certificate
                var isuPemFile = GetIssuerCertificate(certificateRequest, cp);

                using (FileStream intermediate = new FileStream(isuPemFile, FileMode.Open),
                    certificate = new FileStream(crtPemFile, FileMode.Open),
                    chain = new FileStream(chainPemFile, FileMode.Create))
                {
                    certificate.CopyTo(chain);
                    intermediate.CopyTo(chain);
                }

                Log.Debug("CentralSsl {CentralSsl} San {San}", Options.CentralSsl.ToString(), Options.San.ToString());

                if (Options.CentralSsl && Options.San)
                {
                    foreach (var host in allDnsIdentifiers)
                    {
                        Log.Information("Host: {host}", host);
                        crtPfxFile = Path.Combine(Options.CentralSslStore, $"{host}.pfx");

                        Log.Information("Saving Certificate to {crtPfxFile}", crtPfxFile);
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
                                Log.Error("Error exporting archive {@ex}", ex);
                            }
                        }
                    }
                }
                else //Central SSL and San need to save the cert for each hostname
                {
                    Log.Information("Saving Certificate to {crtPfxFile}", crtPfxFile);
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
                            Log.Error("Error exporting archive {@ex}", ex);
                        }
                    }
                }

                cp.Dispose();

                return crtPfxFile;
            }

            Log.Error("Request status = {StatusCode}", certificateRequest.StatusCode);
            throw new Exception($"Request status = {certificateRequest.StatusCode}");
        }
        
        public string GetIssuerCertificate(CertificateRequest certificate, CertificateProvider cp)
        {
            var linksEnum = certificate.Links;
            if (linksEnum == null)
                return null;

            var links = new LinkCollection(linksEnum);
            var upLink = links.GetFirstOrDefault("up");
            if (upLink == null)
                return null;

            var temporaryFileName = Path.GetTempFileName();
            try
            {
                using (var web = new WebClient())
                {
                    var uri = new Uri(new Uri(Options.BaseUri), upLink.Uri);
                    web.DownloadFile(uri, temporaryFileName);
                }

                var cacert = new X509Certificate2(temporaryFileName);
                var sernum = cacert.GetSerialNumberString();

                var cacertDerFile = Path.Combine(Options.CertOutPath, $"ca-{sernum}-crt.der");
                var cacertPemFile = Path.Combine(Options.CertOutPath, $"ca-{sernum}-crt.pem");

                if (!File.Exists(cacertDerFile))
                    File.Copy(temporaryFileName, cacertDerFile, true);

                Log.Information("Saving Issuer Certificate to {cacertPemFile}", cacertPemFile);
                if (File.Exists(cacertPemFile))
                    return cacertPemFile;

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

            return null;
        }
    }
}
