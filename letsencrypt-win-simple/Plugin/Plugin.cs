using ACMESharp;
using ACMESharp.ACME;
using ACMESharp.HTTP;
using ACMESharp.JOSE;
using ACMESharp.PKI;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace LetsEncrypt.ACME.Simple
{
    /// <summary>
    /// To create a new server plugin, simply create a sub-class of Plugin in this project. It will be loaded and run automatically.
    /// </summary>
    internal abstract class Plugin
    {
        private const string BLOCK_SEPARATOR = "\n******************************************************************************";
        private const string INVALID_STATUS = "invalid";
        private const string PENDING_STATUS = "pending";
        private const string VALID_STATUS = "valid";

        public AcmeClient client { get; set; }
        public List<string> AlternativeNames = null;

        /// <summary>
        /// Whether this plugin requires elevated system access
        /// </summary>
        public abstract bool RequiresElevated { get; }

        /// <summary>
        /// Validates that the plugin can run
        /// </summary>
        public abstract bool Validate(Options options);

        /// <summary>
        /// Prompt for plugin-specific options
        /// </summary>
        public abstract bool SelectOptions(Options options);

        /// <summary>
        /// A unique plugin identifier. ("IIS", "Manual", etc.)
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Returns true if this plugin was chosen by the given menu option
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public abstract bool GetSelected(ConsoleKeyInfo key);

        /// <summary>
        /// Generates a list of hosts for which certificates can be created
        /// </summary>
        /// <returns></returns>
        public abstract List<Target> GetTargets(Options options);

        /// <summary>
        /// Prints menu options
        /// </summary>
        public abstract void PrintMenu();

        /// <summary>
        /// The code that is to authorize target, generate cert, install the cert, and setup renewal
        /// </summary>
        /// <param name="binding">The target to process</param>
        public virtual string Auto(Target target, Options options)
        {
            var auth = Authorize(target, options);
            string pfxFilename = null;
            if (auth.Status == VALID_STATUS)
            {
                pfxFilename = GetCertificate(target, client, options);
            }
            return pfxFilename;
        }

        protected virtual Dictionary<string, string> GetConfig(Options options)
        {
            string configFile = Path.GetFullPath((string.IsNullOrEmpty(options.PluginConfig)) ? Name.Replace(" ", "") + ".json" : options.PluginConfig);
            if (File.Exists(configFile))
            {
                string text = File.ReadAllText(configFile);
                var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                return config;
            }
            return new Dictionary<string, string>();
        }

        protected static void RequireNotNull(string field, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(field);
            }
        }

        protected virtual AuthorizationState Authorize(Target target, Options options)
        {
            List<string> dnsIdentifiers = new List<string>();
            if (!options.San)
            {
                dnsIdentifiers.Add(target.Host);
            }
            if (target.AlternativeNames != null)
            {
                dnsIdentifiers.AddRange(target.AlternativeNames);
            }
            List<AuthorizationState> authStatus = new List<AuthorizationState>();

            foreach (var dnsIdentifier in dnsIdentifiers)
            {
                var webRootPath = target.WebRootPath;

                Log.Information(R.Authorizingidentifierusingchallengetype, dnsIdentifier, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                var authzState = client.AuthorizeIdentifier(dnsIdentifier);
                var challenge = client.DecodeChallenge(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
                var httpChallenge = challenge.Challenge as HttpChallenge;

                // We need to strip off any leading '/' in the path
                var filePath = httpChallenge.FilePath;
                if (filePath.StartsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    filePath = filePath.Substring(1);
                }
                var answerPath = webRootPath.StartsWith("ftp")
                    ? string.Format("{0}/{1}", webRootPath, filePath)
                    : Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath, filePath));

                target.Plugin.CreateAuthorizationFile(answerPath, httpChallenge.FileContent);

                target.Plugin.BeforeAuthorize(target, answerPath, httpChallenge.Token);

                var answerUri = new Uri(httpChallenge.FileUrl);

                if (options.Warmup)
                {
                    Console.WriteLine(R.Waitingforsitetowarmup);
                    WarmupSite(answerUri);
                }

                Log.Information(R.AnswerfileshouldnowbeavailableatanswerUri, answerUri);

                try
                {
                    Log.Information(R.Submittinganswer);
                    authzState.Challenges = new AuthorizeChallenge[] { challenge };
                    client.SubmitChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP, true);

                    // Wait on server pending status.
                    int retries = 60; // 5 minutes
                    while (authzState.Status == PENDING_STATUS && retries > 0)
                    {
                        Log.Information(R.Refreshingauthorization);
                        Thread.Sleep(5000);
                        authzState = client.RefreshIdentifierAuthorization(authzState);
                        retries--;
                    }

                    Log.Information(R.AuthorizationResultStatus, authzState.Status);
                    if (authzState.Status == INVALID_STATUS)
                    {
                        Log.Error(R.AuthorizationFailedStatus, authzState.Status);
                        Log.Debug(R.FullErrorDetailsauthzState, authzState);
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(BLOCK_SEPARATOR);

                        Log.Error(R.TheACMEserverwasprobablyunabletoreachanswerUri, answerUri);

                        Console.WriteLine(R.Checkinabrowsertoseetheanswerfile);

                        target.Plugin.OnAuthorizeFail(target);

                        Console.WriteLine(BLOCK_SEPARATOR);
                        Console.ResetColor();
                    }
                    authStatus.Add(authzState);
                }
                finally
                {
                    if (authzState.Status == VALID_STATUS)
                    {
                        target.Plugin.DeleteAuthorization(answerPath, httpChallenge.Token, webRootPath, filePath);
                    }
                }
            }
            foreach (var authState in authStatus)
            {
                if (authState.Status != VALID_STATUS)
                {
                    return authState;
                }
            }
            return new AuthorizationState { Status = VALID_STATUS };
        }
        
        internal virtual void WarmupSite(Uri uri)
        {
            bool retry = false;
            do
            {
                try
                {
                    var request = WebRequest.Create(uri);
                    request.Headers.Add(HttpRequestHeader.UserAgent, LetsEncrypt.CLIENT_NAME);
                    request.Method = "GET";
                    request.Timeout = 120000; //2 minutes
                    request.GetResponse();
                }
                catch (TimeoutException) { retry = true; }
                catch (Exception ex)
                {
                    Log.Error(R.Errorwarmingupsite, ex);
                }
            } while (retry);
        }

        internal virtual string GetCertificate(Target binding, AcmeClient client, Options options)
        {
            var dnsIdentifier = binding.Host;
            var sanList = binding.AlternativeNames;
            List<string> allDnsIdentifiers = new List<string>();

            if (!options.San)
            {
                allDnsIdentifiers.Add(binding.Host);
            }
            if (binding.AlternativeNames != null)
            {
                allDnsIdentifiers.AddRange(binding.AlternativeNames);
            }

            var cp = CertificateProvider.GetProvider();
            var rsaPkp = new RsaPrivateKeyParams();
            try
            {
                if (Properties.Settings.Default.RSAKeyBits >= 1024)
                {
                    rsaPkp.NumBits = Properties.Settings.Default.RSAKeyBits;
                }
                else
                {
                    Log.Warning(R.RSAKeyBitslessthan1024Warning);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(R.UnabletosetRSAKeyBits, ex);
            }

            var rsaKeys = cp.GeneratePrivateKey(rsaPkp);
            var csrDetails = new CsrDetails
            {
                CommonName = allDnsIdentifiers[0],
            };
            if (sanList != null)
            {
                if (sanList.Count > 0)
                {
                    csrDetails.AlternativeNames = sanList;
                }
            }
            var csrParams = new CsrParams
            {
                Details = csrDetails,
            };
            var csr = cp.GenerateCsr(csrParams, rsaKeys, Crt.MessageDigest.SHA256);

            byte[] derRaw;
            using (var bs = new MemoryStream())
            {
                cp.ExportCsr(csr, EncodingFormat.DER, bs);
                derRaw = bs.ToArray();
            }
            var derB64U = JwsHelper.Base64UrlEncode(derRaw);

            Log.Information(R.Requestingcertificate);
            var certRequest = client.RequestCertificate(derB64U);

            Log.Information(R.RequestStatusCode, certRequest.StatusCode);

            if (certRequest.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var keyGenFile = Path.Combine(options.CertOutPath, $"{dnsIdentifier}-gen-key.json");
                var keyPemFile = Path.Combine(options.CertOutPath, $"{dnsIdentifier}-key.pem");
                var csrGenFile = Path.Combine(options.CertOutPath, $"{dnsIdentifier}-gen-csr.json");
                var csrPemFile = Path.Combine(options.CertOutPath, $"{dnsIdentifier}-csr.pem");
                var crtDerFile = Path.Combine(options.CertOutPath, $"{dnsIdentifier}-crt.der");
                var crtPemFile = Path.Combine(options.CertOutPath, $"{dnsIdentifier}-crt.pem");
                var chainPemFile = Path.Combine(options.CertOutPath, $"{dnsIdentifier}-chain.pem");
                string crtPfxFile = null;
                if (!options.CentralSsl)
                {
                    crtPfxFile = Path.Combine(options.CertOutPath, $"{dnsIdentifier}-all.pfx");
                }
                else
                {
                    crtPfxFile = Path.Combine(options.CentralSslStore, $"{dnsIdentifier}.pfx");
                }

                using (var fs = new FileStream(keyGenFile, FileMode.Create))
                    cp.SavePrivateKey(rsaKeys, fs);
                using (var fs = new FileStream(keyPemFile, FileMode.Create))
                    cp.ExportPrivateKey(rsaKeys, EncodingFormat.PEM, fs);
                using (var fs = new FileStream(csrGenFile, FileMode.Create))
                    cp.SaveCsr(csr, fs);
                using (var fs = new FileStream(csrPemFile, FileMode.Create))
                    cp.ExportCsr(csr, EncodingFormat.PEM, fs);

                Log.Information(R.Savingcertificatetofile, crtDerFile);
                using (var file = File.Create(crtDerFile))
                {
                    certRequest.SaveCertificate(file);
                }

                Crt crt;
                using (FileStream source = new FileStream(crtDerFile, FileMode.Open), target = new FileStream(crtPemFile, FileMode.Create))
                {
                    crt = cp.ImportCertificate(EncodingFormat.DER, source);
                    cp.ExportCertificate(crt, EncodingFormat.PEM, target);
                }

                // To generate a PKCS#12 (.PFX) file, we need the issuer's public certificate
                var issuerPemFile = GetIssuerCertificate(certRequest, cp, options);

                using (FileStream intermediate = new FileStream(issuerPemFile, FileMode.Open),
                    certificate = new FileStream(crtPemFile, FileMode.Open),
                    chain = new FileStream(chainPemFile, FileMode.Create))
                {
                    certificate.CopyTo(chain);
                    intermediate.CopyTo(chain);
                }

                if (options.CentralSsl && options.San)
                {
                    foreach (var host in allDnsIdentifiers)
                    {
                        Console.WriteLine(string.Format(R.Host, host));
                        crtPfxFile = Path.Combine(options.CentralSslStore, $"{host}.pfx");

                        Log.Information(R.Savingcertificatetofile, crtPfxFile);
                        using (FileStream source = new FileStream(issuerPemFile, FileMode.Open),
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
                                Log.Error(R.Errorexportingarchive, ex);
                            }
                        }
                    }
                }
                else //Central SSL and San need to save the cert for each hostname
                {
                    Log.Information(R.Savingcertificatetofile, crtPfxFile);
                    using (FileStream source = new FileStream(issuerPemFile, FileMode.Open),
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
                            Log.Error(R.Errorexportingarchive, ex);
                        }
                    }
                }

                cp.Dispose();

                return crtPfxFile;
            }
            Log.Error(R.RequestStatusCode, certRequest.StatusCode);
            throw new Exception(string.Format(R.RequestFailedStatusCode, certRequest.StatusCode));
        }

        internal virtual string GetIssuerCertificate(CertificateRequest certificate, CertificateProvider cp, Options options)
        {
            var linksEnum = certificate.Links;
            if (linksEnum != null)
            {
                var links = new LinkCollection(linksEnum);
                var upLink = links.GetFirstOrDefault("up");
                if (upLink != null)
                {
                    var temporaryFileName = Path.GetTempFileName();
                    try
                    {
                        using (var web = new WebClient())
                        {
                            var uri = new Uri(new Uri(options.BaseUri), upLink.Uri);
                            web.DownloadFile(uri, temporaryFileName);
                        }

                        var cacert = new X509Certificate2(temporaryFileName);
                        var sernum = cacert.GetSerialNumberString();

                        var cacertDerFile = Path.Combine(options.CertOutPath, $"ca-{sernum}-crt.der");
                        var cacertPemFile = Path.Combine(options.CertOutPath, $"ca-{sernum}-crt.pem");

                        if (!File.Exists(cacertDerFile))
                        {
                            File.Copy(temporaryFileName, cacertDerFile, true);
                        }

                        Log.Information(R.Savingissuercertificatetofile, cacertPemFile);
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
                        {
                            File.Delete(temporaryFileName);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Can be used to write out server specific configuration, to handle extensionless files etc.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="answerPath"></param>
        /// <param name="token"></param>
        public virtual void BeforeAuthorize(Target target, string answerPath, string token)
        {
        }

        /// <summary>
        /// Can be used to print out helpful troubleshooting info for the user.
        /// </summary>
        /// <param name="target"></param>
        public virtual void OnAuthorizeFail(Target target)
        {
            Log.Error(R.IISAuthorizeFailedMessage);
        }
        
        public virtual void RunScript(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate, Options options)
        {
            if (!string.IsNullOrWhiteSpace(options.Script))
            {
                if (!string.IsNullOrWhiteSpace(options.ScriptParameters))
                {
                    var parameters = string.Format(options.ScriptParameters, target.Host,
                        Properties.Settings.Default.PFXPassword,
                        pfxFilename, store.Name, certificate.FriendlyName, certificate.Thumbprint);
                    Log.Information(R.RunningScriptwithparameters, options.Script, parameters);
                    Process.Start(options.Script, parameters);
                }
                else
                {
                    Log.Information(R.RunningScript, options.Script);
                    Process.Start(options.Script);
                }
            }
        }

        /// <summary>
        /// Configure the server software to use the certificate.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="options"></param>
        public abstract void Install(Target target, Options options);

        /// <summary>
        /// Should renew the certificate
        /// </summary>
        /// <param name="target"></param>
        public abstract void Renew(Target target, Options options);

        /// <summary>
        /// Should create any directory structure needed and write the file for authorization
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="fileContents">the contents of the file to write</param>
        public virtual void CreateAuthorizationFile(string answerPath, string fileContents)
        {
        }

        /// <summary>
        /// Should delete any authorizations
        /// </summary>
        /// <param name="answerPath">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        /// <param name="webRootPath">the website root path</param>
        /// <param name="filePath">the file path for the authorization file</param>
        public virtual void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
        }
    }
}