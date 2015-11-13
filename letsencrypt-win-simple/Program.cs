using LetsEncrypt.ACME.JOSE;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using System.Threading;
using LetsEncrypt.ACME.PKI;
using System.Security.Cryptography.X509Certificates;
using LetsEncrypt.ACME.HTTP;
using System.Net;
using System.Security.Principal;
using CommandLine.Text;
using CommandLine;

namespace LetsEncrypt.ACME.Simple
{
    class Program
    {
        public static string BaseURI { get; set; } = "https://acme-staging.api.letsencrypt.org/";
        public static string ProductionBaseURI { get; set; } = "https://acme-v01.api.letsencrypt.org/";

        static ServerManager iisManager;
        static string configPath;
        static Settings settings;
        static AcmeClient client;
        static Options options;

        static bool IsElevated => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        static void Main(string[] args)
        {
            //var commandLineParseResult = CommandLine.Parser.Default.ParseArguments<Options>(args);
            //var parsed = commandLineParseResult as Parsed<Options>;
            //if (parsed == null)
            //    return; // not parsed
            //options = parsed.Value;

            Console.WriteLine("Let's Encrypt");

            Console.Write("\nUse production Let's Encrypt server? (Y/N) ");
            if (PromptYesNo())
                BaseURI = ProductionBaseURI;

            Console.WriteLine($"\nACME Server: {BaseURI}");

            settings = new Settings(BaseURI);

            configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LetsEncrypt", CleanFileName(BaseURI));
            Console.WriteLine("Config Folder: " + configPath);
            Directory.CreateDirectory(configPath);

            using (var signer = new RS256Signer())
            {
                signer.Init();

                var signerPath = Path.Combine(configPath, "Signer");
                if (File.Exists(signerPath))
                {
                    Console.WriteLine($"Loading Signer from {signerPath}");
                    using (var signerStream = File.OpenRead(signerPath))
                        signer.Load(signerStream);
                }
                else
                {
                    Console.WriteLine("Saving Signer");
                    using (var signerStream = File.OpenWrite(signerPath))
                        signer.Save(signerStream);
                }

                using (client = new AcmeClient(new Uri(BaseURI), new AcmeServerDirectory(), signer))
                {
                    client.Init();
                    Console.WriteLine("\nGetting AcmeServerDirectory");
                    client.GetDirectory(true);

                    var registrationPath = Path.Combine(configPath, "Registration");
                    if (File.Exists(registrationPath))
                    {
                        Console.WriteLine($"Loading Registration from {registrationPath}");
                        using (var registrationStream = File.OpenRead(registrationPath))
                            client.Registration = AcmeRegistration.Load(registrationStream);
                    }
                    else
                    {
                        Console.WriteLine("Calling Register");
                        var registration = client.Register(new string[] { });

                        Console.WriteLine($"Do you agree to {registration.TosLinkUri}? (Y/N) ");
                        if (!PromptYesNo())
                            return;

                        Console.WriteLine("Updating Registration");
                        client.UpdateRegistration(true, true);

                        Console.WriteLine("Saving Registration");
                        using (var registrationStream = File.OpenWrite(registrationPath))
                            client.Registration.Save(registrationStream);
                    }

                    CheckRenewals();

                    Console.WriteLine("\nScanning IIS 7 Site Bindings for Hosts (Elevated Permissions Required)");
                    if (!IsElevated)
                    {
                        Console.WriteLine("Elevated Permissions Required. Please run under an administrator console.");
                        return;
                    }

                    using (iisManager = new ServerManager())
                    {
                        var bindings = GetHostNames();
                        if (bindings.Count == 0)
                        {
                            Console.WriteLine("No IIS bindings with host names were found. Please add one using IIS Manager. A host name and site path are required to verify domain ownership.");
                            return;
                        }

                        Console.WriteLine("IIS Bindings");
                        var count = 1;
                        foreach (var binding in bindings)
                        {
                            Console.WriteLine($" {count}: {binding}");
                            count++;
                        }

                        Console.WriteLine();
                        Console.WriteLine(" A: Get Certificates for All Bindings");
                        Console.WriteLine(" Q: Quit");
                        Console.Write("Which binding do you want to get a cert for: ");
                        var response = Console.ReadLine();
                        switch (response.ToLowerInvariant())
                        {
                            case "a":
                                foreach (var binding in bindings)
                                {
                                    Auto(binding);
                                }
                                break;
                            case "q":
                                return;
                            default:
                                var bindingId = 0;
                                if (Int32.TryParse(response, out bindingId))
                                {
                                    bindingId--;
                                    if (bindingId >= 0 && bindingId < bindings.Count)
                                    {
                                        var binding = bindings[bindingId];
                                        Auto(binding);
                                    }
                                }
                                break;
                        }
                    }
                }
            }

#if DEBUG
            Console.WriteLine("Press enter to continue.");
            Console.ReadLine();
#endif
        }

        static string CleanFileName(string fileName) => Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));

        static bool PromptYesNo()
        {
            while (true)
            {
                var response = Console.ReadKey(true);
                if (response.Key == ConsoleKey.Y)
                    return true;
                if (response.Key == ConsoleKey.N)
                    return false;
                Console.WriteLine("Please press Y or N.");
            }
        }

        static List<TargetBinding> GetHostNames()
        {
            var result = new List<TargetBinding>();

            foreach (var site in iisManager.Sites)
            {
                foreach (var binding in site.Bindings)
                {
                    if (!String.IsNullOrEmpty(binding.Host) && binding.Protocol == "http")
                        result.Add(new TargetBinding() { SiteId = site.Id, Host = binding.Host, PhysicalPath = site.Applications["/"].VirtualDirectories["/"].PhysicalPath });
                }
            }
            return result;
        }

        static void Auto(TargetBinding siteHost)
        {
            var dnsIdentifier = siteHost.Host;
            var auth = Authorize(dnsIdentifier, siteHost.PhysicalPath);
            if (auth.Status == "valid")
            {
                GetCertificate(siteHost, dnsIdentifier);
            }
        }

        static void GetCertificate(TargetBinding binding, string dnsIdentifier)
        {
            var rsaKeys = CsrHelper.GenerateRsaPrivateKey();
            var csrDetails = new CsrHelper.CsrDetails
            {
                CommonName = dnsIdentifier
            };
            var csr = CsrHelper.GenerateCsr(csrDetails, rsaKeys);
            byte[] derRaw;
            using (var bs = new MemoryStream())
            {
                csr.ExportAsDer(bs);
                derRaw = bs.ToArray();
            }
            var derB64u = JwsHelper.Base64UrlEncode(derRaw);

            Console.WriteLine($"\nRequesting Certificate");
            var certRequ = client.RequestCertificate(derB64u);

            Console.WriteLine($" Request Status: {certRequ.StatusCode}");

            //Console.WriteLine($"Refreshing Cert Request");
            //client.RefreshCertificateRequest(certRequ);

            if (certRequ.StatusCode == System.Net.HttpStatusCode.Created)
            {
                var keyGenFile = $"{dnsIdentifier}-gen-key.json";
                var keyPemFile = $"{dnsIdentifier}-key.pem";
                var csrGenFile = $"{dnsIdentifier}-gen-csr.json";
                var csrPemFile = $"{dnsIdentifier}-csr.pem";
                var crtDerFile = $"{dnsIdentifier}-crt.der";
                var crtPemFile = $"{dnsIdentifier}-crt.pem";
                var crtPfxFile = $"{dnsIdentifier}-all.pfx";

                using (var fs = new FileStream(keyGenFile, FileMode.Create))
                {
                    rsaKeys.Save(fs);
                    File.WriteAllText(keyPemFile, rsaKeys.Pem);
                }
                using (var fs = new FileStream(csrGenFile, FileMode.Create))
                {
                    csr.Save(fs);
                    File.WriteAllText(csrPemFile, csr.Pem);
                }

                Console.WriteLine($" Saving Certificate to {crtDerFile}");
                using (var file = File.Create(crtDerFile))
                    certRequ.SaveCertificate(file);

                using (FileStream source = new FileStream(crtDerFile, FileMode.Open), target = new FileStream(crtPemFile, FileMode.Create))
                {
                    CsrHelper.Crt.ConvertDerToPem(source, target);
                }

                // can't create a pfx until we get an irsPemFile, which seems to be some issuer cert thing.
                var isrPemFile = GetIssuerCertificate(certRequ);

                Console.WriteLine($" Saving Certificate to {crtPfxFile} (with no password set)");
                CsrHelper.Crt.ConvertToPfx(keyPemFile, crtPemFile, isrPemFile, crtPfxFile, FileMode.Create);

                InstallCertificate(crtPfxFile, binding.GetSite(iisManager), dnsIdentifier);
            }
        }

        static void InstallCertificate(string pfxFilename, Site site, string host)
        {
            Console.WriteLine($"\nDo you want to install the .pfx into the Certificate Store? (Y/N) ");
            if (!PromptYesNo())
                return;

            Console.WriteLine($" Opening Certificate Store");
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadWrite);

            Console.WriteLine($" Loading .pfx");
            X509Certificate2 certificate = new X509Certificate2(pfxFilename, "");

            Console.WriteLine($" Adding Certificate to Store");
            store.Add(certificate);

            Console.WriteLine($" Closing Certificate Store");
            store.Close();

            Console.WriteLine($"\nDo you want to add an https IIS binding? (Y/N) ");
            if (!PromptYesNo())
                return;

            Console.WriteLine($" Adding https Binding");
            var binding = site.Bindings.Add(":443:" + host, certificate.GetCertHash(), store.Name);
            binding.Protocol = "https";

            Console.WriteLine($" Commiting binding changes to IIS");
            iisManager.CommitChanges();
        }

        static void ScheduleRenewal(TargetBinding siteHost)
        {

        }

        static void CheckRenewals()
        {
            Console.WriteLine("Checking Renewals");

            var renewals = settings.LoadRenewals();
            if (renewals.Count == 0)
                Console.WriteLine(" No scheduled renewals found.");

            var now = DateTime.UtcNow;
            foreach (var renewal in renewals)
            {
                if (renewal.Date < now)
                {
                    RenewCertificate(renewal);
                }
            }
        }

        static void RenewCertificate(ScheduledRenewal renewal)
        {
            Console.WriteLine($"Renewing certificate for {renewal}");
            Auto(renewal.Binding);
        }

        static string GetIssuerCertificate(CertificateRequest certificate)
        {
            var linksEnum = certificate.Links;
            if (linksEnum != null)
            {
                var links = new LinkCollection(linksEnum);
                var upLink = links.GetFirstOrDefault("up");
                if (upLink != null)
                {
                    var tmp = Path.GetTempFileName();
                    try
                    {
                        using (var web = new WebClient())
                        {
                            //if (v.Proxy != null)
                            //    web.Proxy = v.Proxy.GetWebProxy();

                            var uri = new Uri(new Uri(BaseURI), upLink.Uri);
                            web.DownloadFile(uri, tmp);
                        }

                        var cacert = new X509Certificate2(tmp);
                        var sernum = cacert.GetSerialNumberString();
                        var tprint = cacert.Thumbprint;
                        var sigalg = cacert.SignatureAlgorithm?.FriendlyName;
                        var sigval = cacert.GetCertHashString();

                        var cacertDerFile = $"ca-{sernum}-crt.der";
                        var cacertPemFile = $"ca-{sernum}-crt.pem";

                        if (!File.Exists(cacertDerFile))
                            File.Copy(tmp, cacertDerFile, true);

                        Console.WriteLine($" Saving Issuer Certificate to {cacertPemFile}");
                        if (!File.Exists(cacertPemFile))
                            CsrHelper.Crt.ConvertDerToPem(cacertDerFile, cacertPemFile);

                        return cacertPemFile;
                    }
                    finally
                    {
                        if (File.Exists(tmp))
                            File.Delete(tmp);
                    }
                }
            }

            return null;
        }

        const string webConfig = @"<?xml version = ""1.0"" encoding=""UTF-8""?>
 <configuration>
     <system.webServer>
         <staticContent>
             <mimeMap fileExtension = "".*"" mimeType=""text/json"" />
         </staticContent>
     </system.webServer>
 </configuration>";

        //<handlers>
        //    <remove name = "StaticFile" />
        //    < add name="StaticFile" path="*." verb="*" type="" modules="StaticFileModule,DefaultDocumentModule,DirectoryListingModule" scriptProcessor="" resourceType="Either" requireAccess="Read" allowPathInfo="false" preCondition="" responseBufferLimit="4194304" />
        //</handlers>

        static AuthorizationState Authorize(string dnsIdentifier, string webRootPath)
        {
            Console.WriteLine($"\nAuthorizing Identifier {dnsIdentifier} Using Challenge Type {AcmeProtocol.CHALLENGE_TYPE_HTTP}");
            var authzState = client.AuthorizeIdentifier(dnsIdentifier);
            var challenge = client.GenerateAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP);
            var answerPath = Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath, challenge.ChallengeAnswer.Key));

            Console.WriteLine($" Writing challenge answer to {answerPath}");
            var directory = Path.GetDirectoryName(answerPath);
            Directory.CreateDirectory(directory);
            File.WriteAllText(answerPath, challenge.ChallengeAnswer.Value);

            var webConfigPath = Path.Combine(directory, "web.config");
            Console.WriteLine($" Writing web.config to add extensionless mime type to {webConfigPath}");
            File.WriteAllText(webConfigPath, webConfig);

            var answerUri = new Uri(new Uri("http://" + dnsIdentifier), challenge.ChallengeAnswer.Key);
            Console.WriteLine($" Answer should now be browsable at {answerUri}");

            try
            {
                Console.WriteLine(" Submitting answer");
                // This always throws throw new InvalidOperationException("challenge answer has not been generated"); because the authoState.Challenge list isn't changing for some reason

                authzState.Challenges = new AuthorizeChallenge[] { challenge };
                client.SubmitAuthorizeChallengeAnswer(authzState, AcmeProtocol.CHALLENGE_TYPE_HTTP, true);

                // so I pulled the core of SubmitAuthorizeChallengeAnswer into it's own method that I can call directly
                //client.SubmitAuthorizeChallengeAnswer(challenge, true);

                // have to loop to wait for server to stop being pending.
                // TODO: put timeout/retry limit in this loop
                while (authzState.Status == "pending")
                {
                    Console.WriteLine(" Refreshing authorization");
                    Thread.Sleep(1000); // this has to be here to give ACME server a chance to think
                    var newAuthzState = client.RefreshIdentifierAuthorization(authzState);
                    if (newAuthzState.Status != "pending")
                        authzState = newAuthzState;
                }

                Console.WriteLine($" Authorization RESULT: {authzState.Status}");
                if (authzState.Status == "invalid")
                {
                    Console.WriteLine("\n******************************************************************************");
                    Console.WriteLine($"The ACME server was probably unable to reach {answerUri}");

                    Console.WriteLine(@"
Check in a browser to see if the answer file is being served correctly.

This could be caused by IIS not being setup to handle extensionless static
files. Here's how to fix that:
1. In IIS manager goto Site/Server->Handler Mappings->View Ordered List
2. Move the StaticFile mapping above the ExtensionlessUrlHandler mappings.
(like this http://i.stack.imgur.com/nkvrL.png)
******************************************************************************");
                }

                if (authzState.Status == "valid")
                {
                    var authPath = Path.Combine(configPath, dnsIdentifier + ".auth");
                    Console.WriteLine($" Saving authorization record to: {authPath}");
                    using (var authStream = File.Create(authPath))
                        authzState.Save(authStream);
                }

                return authzState;
            }
            finally
            {
                //Console.WriteLine(" Deleting answer");
                //File.Delete(answerPath);
            }
        }
    }
}
