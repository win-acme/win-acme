using Newtonsoft.Json;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients
{
    internal class AcmeDnsClient
    {
        private readonly ProxyService _proxy;
        private readonly LookupClientProvider _dnsClient;
        private readonly ILogService _log;
        private readonly string _dnsConfigPath;
        private readonly Uri _baseUri;
        private readonly IInputService? _input;

        public AcmeDnsClient(LookupClientProvider dnsClient, ProxyService proxy, ILogService log,
                             ISettingsService settings, IInputService? input, Uri baseUri)
        {
            _baseUri = baseUri;
            _proxy = proxy;
            _dnsClient = dnsClient;
            _log = log;
            _input = input;
            var configDir = new DirectoryInfo(settings.Client.ConfigurationPath);
            var legacyPath = Path.Combine(configDir.FullName, "acme-dns", _baseUri.CleanUri());
            var legacyDirectory = new DirectoryInfo(legacyPath);
            if (!legacyDirectory.Exists)
            {
                // Go up one level so that multiple ACME servers
                // can share the same acme-dns registrations
                var parentPath = Path.Combine(configDir.Parent.FullName, "acme-dns", _baseUri.CleanUri());
                var parentDirectory = new DirectoryInfo(parentPath);
                if (!parentDirectory.Exists)
                {
                    parentDirectory.Create();
                }
                _dnsConfigPath = parentPath;
            }
            else
            {
                _dnsConfigPath = legacyPath;
            }
            _log.Debug("Using {path} for acme-dns configuration", _dnsConfigPath);
        }

        /// <summary>
        /// Check for existing registration linked to the domain, or create a new one
        /// </summary>
        /// <param name="domain"></param>
        public async Task<bool> EnsureRegistration(string domain, bool interactive)
        {
            var oldReg = RegistrationForDomain(domain);
            if (oldReg == null)
            {
  
                if (interactive && _input != null)
                {
                    _log.Information($"Creating new acme-dns registration for domain {domain}");
                    var newReg = await Register();
                    if (newReg != null)
                    {
                        // Verify correctness
                        _input.CreateSpace();
                        _input.Show("Domain", domain);
                        _input.Show("Record", $"_acme-challenge.{domain}");
                        _input.Show("Type", "CNAME");
                        _input.Show("Content", newReg.Fulldomain + ".");
                        _input.Show("Note", "Some DNS control panels add the final dot automatically. Only one is required.");
                        if (!await _input.Wait("Please press <Enter> after you've created and verified the record"))
                        {
                            _log.Warning("User aborted");
                            return false;
                        }
                        if (await VerifyRegistration(domain, newReg.Fulldomain, interactive))
                        {
                            await File.WriteAllTextAsync(FileForDomain(domain), JsonConvert.SerializeObject(newReg));
                            return true;
                        }
                    }
                }
                else
                {
                    _log.Error("No previous acme-dns registration found for domain {domain}", domain);
                }
            }
            else
            {
                _log.Information($"Existing acme-dns registration for domain {domain} found");
                _log.Information($"Record: _acme-challenge.{domain}");
                _log.Information("CNAME: " + oldReg.Fulldomain);
                return await VerifyRegistration(domain, oldReg.Fulldomain, interactive);
            }
            return false;
        }

        private async Task<bool> VerifyRegistration(string domain, string fullDomain, bool interactive)
        {
            var round = 0;
            do
            {
                if (await VerifyCname(domain, fullDomain, round++))
                {
                    return true;
                }
                else if (interactive && _input != null)
                {
                    if (!await _input.PromptYesNo("Press 'Y' or <Enter> to retry, or 'N' to skip this step.", true))
                    {
                        _log.Warning("Verification of acme-dns configuration skipped.");
                        return true;
                    }
                } 
                else
                {
                    return false;
                }
            }
            while (true);
        }
        /// <summary>
        /// Verify configuration
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cname"></param>
        /// <returns></returns>
        private async Task<bool> VerifyCname(string domain, string expected, int round)
        {
            try
            {
                var authority = await _dnsClient.GetAuthority(domain, round, false);
                var result = authority.Nameservers.ToList();
                _log.Debug("Configuration will now be checked at name servers: {address}",
                    string.Join(", ", result.Select(x => x.IpAddress)));

                // Parallel queries
                var answers = await Task.WhenAll(result.Select(client => client.GetCname($"_acme-challenge.{domain}")));

                // Loop through results
                for (var i = 0; i < result.Count(); i++)
                {
                    var currentClient = result[i];
                    var currentResult = answers[i];
                    if (string.Equals(expected, currentResult, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _log.Verbose("Verification of CNAME record successful at server {server}", currentClient.IpAddress);
                    }
                    else
                    {
                        _log.Warning("Verification failed, {domain} found value {found} but expected {expected} at server {server}", 
                            $"_acme-challenge.{domain}",
                            currentResult ?? "(null)", 
                            expected, 
                            currentClient.IpAddress);
                        return false;
                    }
                }
                _log.Information("Verification of acme-dns configuration succesful.");
                return true;
            } 
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to verify acme-dns configuration.");
                return false;
            }
        }

        private string FileForDomain(string domain) => Path.Combine(_dnsConfigPath, $"{domain.CleanPath()}.json");

        private RegisterResponse? RegistrationForDomain(string domain)
        {
            var file = FileForDomain(domain);
            if (!File.Exists(file))
            {
                return null;
            }
            try
            {
                var text = File.ReadAllText(file);
                return JsonConvert.DeserializeObject<RegisterResponse>(text);
            }
            catch
            {
                _log.Error($"Unable to read acme-dns registration from {file}");
                return null;
            }
        }

        private async Task<RegisterResponse?> Register()
        {
            using var client = Client();
            try
            {
                var response = await client.PostAsync($"register", new StringContent(""));
                return JsonConvert.DeserializeObject<RegisterResponse>(await response.Content.ReadAsStringAsync());
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating acme-dns registration");
                return null;
            }
        }

        public async Task<bool> Update(string domain, string token)
        {
            var reg = RegistrationForDomain(domain);
            if (reg == null)
            {
                _log.Error("No registration found for domain {domain}", domain);
                return false;
            }
            if (reg.Fulldomain == null)
            {
                _log.Error("Registration for domain {domain} appears invalid", domain);
                return false;
            }
            if (!await VerifyCname(domain, reg.Fulldomain, 0))
            {
                _log.Warning("Registration for domain {domain} appears invalid", domain);
            }
            using var client = Client();
            client.DefaultRequestHeaders.Add("X-Api-User", reg.UserName);
            client.DefaultRequestHeaders.Add("X-Api-Key", reg.Password);
            var request = new UpdateRequest()
            {
                Subdomain = reg.Subdomain,
                Token = token
            };
            try
            {
                _log.Debug("Sending update request to acme-dns server at {baseUri} for domain {domain}", _baseUri, domain);
                await client.PostAsync(
                    $"update", 
                    new StringContent(
                        JsonConvert.SerializeObject(request), 
                        Encoding.UTF8, 
                        "application/json"));
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error sending update request to acme-dns server at {baseUri} for domain {domain}", _baseUri, domain);
                return false;
            }
        }

        /// <summary>
        /// Construct common WebClient
        /// </summary>
        /// <returns></returns>
        private HttpClient Client()
        {
            var httpClient = _proxy.GetHttpClient();
            var uri = _baseUri;
            httpClient.BaseAddress = uri;
            if (uri.UserInfo != null)
            {
                var authInfo = Convert.ToBase64String(Encoding.ASCII.GetBytes(uri.UserInfo));
                var authHeader = new AuthenticationHeaderValue("Basic", authInfo);
                httpClient.DefaultRequestHeaders.Authorization = authHeader;
            }
            return httpClient;
        }

        public class UpdateRequest
        {
            [JsonProperty(PropertyName = "subdomain")]
            public string Subdomain { get; set; } = "";
            [JsonProperty(PropertyName = "txt")]
            public string Token { get; set; } = "";
        }

        public class RegisterResponse
        {
            [JsonProperty(PropertyName = "username")]
            public string UserName { get; set; } = "";
            [JsonProperty(PropertyName = "password")]
            public string Password { get; set; } = "";
            [JsonProperty(PropertyName = "fulldomain")]
            public string Fulldomain { get; set; } = "";
            [JsonProperty(PropertyName = "subdomain")]
            public string Subdomain { get; set; } = "";
        }
    }
}
