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

        public AcmeDnsClient(LookupClientProvider dnsClient, ProxyService proxy, ILogService log, ISettingsService settings, IInputService? input, Uri baseUri)
        {
            _baseUri = baseUri;
            _proxy = proxy;
            _dnsClient = dnsClient;
            _log = log;
            _input = input;
            _dnsConfigPath = Path.Combine(settings.Client.ConfigurationPath, "acme-dns", _baseUri.CleanUri());
            var di = new DirectoryInfo(_dnsConfigPath);
            if (!di.Exists)
            {
                di.Create();
            }
            _log.Verbose("Using {path} for acme-dns configuration", _dnsConfigPath);
        }

        /// <summary>
        /// Check for existing registration linked to the domain, or create a new one
        /// </summary>
        /// <param name="domain"></param>
        public async Task<bool> EnsureRegistration(string domain, bool interactive)
        {
            var round = 0;
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

                        do
                        {
                            _input.Show("Domain", domain, true);
                            _input.Show("Record", $"_acme-challenge.{domain}");
                            _input.Show("Type", "CNAME");
                            _input.Show("Content", newReg.Fulldomain + ".");
                            _input.Show("Note", "Some DNS control panels add the final dot automatically. Only one is required.");
                            if (!await _input.Wait("Please press enter after you've created and verified the record"))
                            {
                                throw new Exception("User aborted");
                            }
                        }
                        while (!await VerifyConfiguration(domain, newReg.Fulldomain, round++));
                        File.WriteAllText(FileForDomain(domain), JsonConvert.SerializeObject(newReg));
                        return true;
                    }
                }
                else
                {
                    _log.Error("No previous acme-dns registration found for domain {domain}", domain);
                    return false;
                }
            }
            else
            {
                _log.Information($"Existing acme-dns registration for domain {domain} found");
                _log.Information($"Record: _acme-challenge.{domain}");
                _log.Information("CNAME: " + oldReg.Fulldomain);
                while (!await VerifyConfiguration(domain, oldReg.Fulldomain, round++))
                {
                    if (interactive && _input != null)
                    {
                        if (!await _input.Wait("Please press enter after you've corrected the record."))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Verify configuration
        /// </summary>
        /// <param name="domain"></param>
        /// <param name="cname"></param>
        /// <returns></returns>
        private async Task<bool> VerifyConfiguration(string domain, string expected, int round)
        {
            var dnsClients = await _dnsClient.GetClients(domain, round);
            _log.Debug("Configuration will now be checked at name servers: {address}",
                string.Join(", ", dnsClients.Select(x => x.IpAddress)));

            // Parallel queries
            var answers = await Task.WhenAll(dnsClients.Select(client => client.LookupClient.QueryAsync($"_acme-challenge.{domain}", DnsClient.QueryType.CNAME)));

            // Loop through results
            for (var i = 0; i < dnsClients.Count(); i++)
            {
                var currentClient = dnsClients[i];
                var currentResult = answers[i];
                var value = currentResult.Answers.CnameRecords().
                  Select(cnameRecord => cnameRecord?.CanonicalName?.Value?.TrimEnd('.')).
                  Where(txtRecord => txtRecord != null).
                  FirstOrDefault();

                if (string.Equals(expected, value, StringComparison.CurrentCultureIgnoreCase))
                {
                    _log.Verbose("Verification of CNAME record successful at server {server}", currentClient.IpAddress);
                }
                else
                {
                    _log.Warning("Verification failed, {domain} found value {found} but expected {expected} at server {server}", 
                        $"_acme-challenge.{domain}",
                        value ?? "(null)", 
                        expected, 
                        currentClient.IpAddress);
                    return false;
                }
            }
            _log.Debug("Verification of CNAME record successful");
            return true;
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

        public async Task Update(string domain, string token)
        {
            var reg = RegistrationForDomain(domain);
            if (reg == null)
            {
                _log.Error("No registration found for domain {domain}", domain);
                return;
            }
            if (reg.Fulldomain == null)
            {
                _log.Error("Registration for domain {domain} appears invalid", domain);
                return;
            }
            if (!await VerifyConfiguration(domain, reg.Fulldomain, 0))
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
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error sending update request to acme-dns server at {baseUri} for domain {domain}", _baseUri, domain);
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
