using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
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

        private readonly AzureOptions _options;
        public Azure(AzureOptions options, LookupClientProvider dnsClient, ILogService log) : base(dnsClient, log) => _options = options;

        public override async Task CreateRecord(string recordName, string token)
        {
            var client = await GetClient();
            var zone = await GetHostedZone(recordName);
            var subDomain = recordName.Substring(0, recordName.LastIndexOf(zone)).TrimEnd('.');

            // Create record set parameters
            var recordSetParams = new RecordSet
            {
                TTL = 3600,
                TxtRecords = new List<TxtRecord>
                {
                    new TxtRecord(new[] { token })
                }
            };

            await client.RecordSets.CreateOrUpdateAsync(_options.ResourceGroupName,
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
                var serviceCreds = await ApplicationTokenProvider.LoginSilentAsync(
                    _options.TenantId,
                    _options.ClientId,
                    _options.Secret.Value);
                _azureDnsClient = new DnsManagementClient(serviceCreds)
                {
                    SubscriptionId = _options.SubscriptionId
                };
            }
            return _azureDnsClient;
        }

        private async Task<string> GetHostedZone(string recordName)
        {
            var client = await GetClient();
            var domainName = _dnsClientProvider.DomainParser.GetRegisterableDomain(recordName);
            var response = await client.Zones.ListByResourceGroupAsync(_options.ResourceGroupName);
            var hostedZone = response.Select(zone =>
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
            OrderByDescending(x => x.fit).
            FirstOrDefault();

            if (hostedZone != null)
            {
                return hostedZone.zone.Name;
            }

            _log.Error($"Can't find hosted zone for domain {domainName}");
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
