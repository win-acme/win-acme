using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace PKISharp.WACS.Clients
{
    class AcmeDnsClient
    {
        private ProxyService _proxy;
        private ILogService _log;
        private string _dnsConfigPath;
        private string _baseUri;
        private IInputService _input;

        public AcmeDnsClient(ProxyService proxy, ILogService log, ISettingsService settings, IInputService input, string baseUri)
        {
            _baseUri = baseUri;
            _proxy = proxy;
            _log = log;
            _input = input;
            _dnsConfigPath = Path.Combine(settings.ConfigPath, "acme-dns", _baseUri.CleanBaseUri());
            var di = new DirectoryInfo(_dnsConfigPath);
            if (!di.Exists) {
                di.Create();
            }
            _log.Verbose("Using {path} for acme-dns configuration", _dnsConfigPath);
        }

        /// <summary>
        /// Check for existing registration linked to the domain, or create a new one
        /// </summary>
        /// <param name="domain"></param>
        public void EnsureRegistration(string domain)
        {
            var oldReg = RegistrationForDomain(domain);
            if (oldReg == null)
            {
                _log.Information($"Creating new acme-dns registration for domain {domain}");
                var newReg = Register();
                if (newReg != null)
                {
                    if (_input.Wait($"CNAME _acme-challenge.{domain} to {newReg.Fulldomain} and press enter..."))
                    {
                        File.WriteAllText(FileForDomain(domain), JsonConvert.SerializeObject(newReg));
                    }
                    else
                    {
                        throw new Exception("User aborted");
                    }
                }
                else
                {
                    throw new Exception("Unable to create acme-dns registration");
                }
            }
            else
            {
                _log.Information($"Existing acme-dns registration for domain {domain} found");
                _log.Information($"_acme-challenge.{domain} should be CNAME'd to {oldReg.Fulldomain}");
            }     
        }

        private string FileForDomain(string domain) 
        {
            return Path.Combine(_dnsConfigPath, $"{domain.CleanBaseUri()}.json");
        }

        private RegisterResponse RegistrationForDomain(string domain)
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

        private RegisterResponse Register()
        {
            _log.Information("Creating new acme-dns registration");
            WebClient client = Client();
            try
            {
                var response = client.UploadString($"/register", "");
                return JsonConvert.DeserializeObject<RegisterResponse>(response);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating acme-dns registration");
                return null;
            }
        }

        public bool Update(string domain, string token)
        {
            var reg = RegistrationForDomain(domain);
            if (reg == null)
            {
                _log.Error("No registration found for domain {domain}", domain);
                return false;
            }
            var client = Client();
            client.Headers.Add("X-Api-User", reg.UserName);
            client.Headers.Add("X-Api-Key", reg.Password);
            client.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            var request = new UpdateRequest()
            {
                Subdomain = reg.Subdomain,
                Token = token
            };
            try
            {
                client.UploadString($"/update", JsonConvert.SerializeObject(request));
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error sending update request to acme-dns for domain {domain}", domain);
                return false;
            } 
            return true;
        }

        /// <summary>
        /// Construct common WebClient
        /// </summary>
        /// <returns></returns>
        private WebClient Client()
        {
            var x = new WebClient
            {
                Proxy = _proxy.GetWebProxy(),
                BaseAddress = _baseUri,
            };
            return x;
        }

        public class UpdateRequest
        {
            [JsonProperty(PropertyName = "subdomain")]
            public string Subdomain { get; set; }
            [JsonProperty(PropertyName = "txt")]
            public string Token { get; set; }
        }

        public class RegisterResponse
        {
            [JsonProperty(PropertyName="username")]
            public string UserName { get; set; }
            [JsonProperty(PropertyName = "password")]
            public string Password { get; set; }
            [JsonProperty(PropertyName = "fulldomain")]
            public string Fulldomain { get; set; }
            [JsonProperty(PropertyName = "subdomain")]
            public string Subdomain { get; set; }
        }
    }
}
