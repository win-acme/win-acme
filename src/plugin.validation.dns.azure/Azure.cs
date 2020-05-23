using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Azure : DnsValidation<Azure>
    {
        private DnsManagementClient _azureDnsClient;
        private readonly DomainParseService _domainParser;
        private readonly ProxyService _proxyService;

        private readonly AzureOptions _options;
        public Azure(AzureOptions options,
            DomainParseService domainParser,
            LookupClientProvider dnsClient, 
            ProxyService proxyService,
            ILogService log, 
            ISettingsService settings)
            : base(dnsClient, log, settings)
        {
            _options = options;
            _domainParser = domainParser;
            _proxyService = proxyService;
        }

        public override async Task CreateRecord(string recordName, string token)
        {
            var client = await GetClient();
            var zone = await GetHostedZone(recordName);
            if (string.IsNullOrEmpty(zone))
            {
                return;
            }

            var subDomain = recordName.Substring(0, recordName.LastIndexOf(zone)).TrimEnd('.');

            // Create record set parameters
            var recordSetParams = new RecordSet
            {
                TTL = 0,
                TxtRecords = new List<TxtRecord>
                {
                    new TxtRecord(new[] { token })
                }
            };

            _ = await client.RecordSets.CreateOrUpdateAsync(_options.ResourceGroupName,
                zone,
                subDomain,
                RecordType.TXT,
                recordSetParams);
        }

        private async Task<DnsManagementClient> GetClient()
        {
            if (_azureDnsClient == null)
            {
                // Build the service credentials and DNS management client
                ServiceClientCredentials credentials;

                // Decide between Managed Service Identity (MSI) and service principal with client credentials
                if (_options.UseMsi)
                {
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://management.azure.com/");
                    credentials = new TokenCredentials(accessToken);
                }
                else
                {
                    credentials = await ApplicationTokenProvider.LoginSilentAsync(
                        _options.TenantId,
                        _options.ClientId,
                        _options.Secret.Value);
                }
                
                _azureDnsClient = new DnsManagementClient(credentials, _proxyService.GetHttpClient(), true)
                {
                    SubscriptionId = _options.SubscriptionId
                };
            }
            return _azureDnsClient;
        }

        private async Task<string> GetHostedZone(string recordName)
        {
            var client = await GetClient();
            var domainName = _domainParser.GetDomain(recordName);
            var zones = new List<Zone>();
            var response = await client.Zones.ListByResourceGroupAsync(_options.ResourceGroupName);
            zones.AddRange(response);
            while (!string.IsNullOrEmpty(response.NextPageLink))
            {
                response = await client.Zones.ListByResourceGroupNextAsync(response.NextPageLink);
            }
            _log.Debug("Found {count} hosted zones in Azure Resource Group {rg}", zones, _options.ResourceGroupName);

            var hostedZone = zones.Select(zone =>
            {
                var fit = 0;
                var name = zone.Name.TrimEnd('.').ToLowerInvariant();
                if (recordName.ToLowerInvariant().EndsWith(name))
                {
                    // If there is a zone for a.b.c.com (4) and one for c.com (2)
                    // then the former is a better (more specific) match than the 
                    // latter, so we should use that
                    fit = name.Split('.').Count();
                }
                return new { zone, fit };
            }).
            Where(x => x.fit > 0).
            OrderByDescending(x => x.fit).
            FirstOrDefault();

            if (hostedZone != null)
            {
                return hostedZone.zone.Name;
            }

            _log.Error(
                "Can't find hosted zone for {domainName} in resource group {ResourceGroupName}", 
                domainName,
                _options.ResourceGroupName);

            return null;
        }

        public override async Task DeleteRecord(string recordName, string token)
        {
            var client = await GetClient();
            var zone = await GetHostedZone(recordName);
            var subDomain = recordName.Substring(0, recordName.LastIndexOf(zone)).TrimEnd('.');
            await client.RecordSets.DeleteAsync(
                _options.ResourceGroupName,
                zone,
                subDomain,
                RecordType.TXT);
        }
    }
}
