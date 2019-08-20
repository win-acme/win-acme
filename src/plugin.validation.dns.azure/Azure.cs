using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Azure : DnsValidation<AzureOptions, Azure>
    {
	    private readonly DnsManagementClient _azureDnsClient;

        public Azure(
	        Target target, 
	        AzureOptions options,
            LookupClientProvider dnsClient,
	        ILogService log, 
	        string identifier) : 
            base(dnsClient, log, options, identifier)
        {
	        // Build the service credentials and DNS management client
            var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(
                _options.TenantId,
                _options.ClientId,
                _options.Secret.Value).Result;
            _azureDnsClient = new DnsManagementClient(serviceCreds) { SubscriptionId = _options.SubscriptionId };
        }

        public override void CreateRecord(string recordName, string token)
        {
            var zone = GetHostedZone(recordName);
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

            _azureDnsClient.RecordSets.CreateOrUpdate(_options.ResourceGroupName,
                zone,
                subDomain,
                RecordType.TXT, 
                recordSetParams);
        }

        private string GetHostedZone(string recordName)
        {
            var domainName = _dnsClientProvider.DomainParser.GetRegisterableDomain(recordName);
            var response = _azureDnsClient.Zones.ListByResourceGroup(_options.ResourceGroupName);
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

        public override void DeleteRecord(string recordName, string token)
        {
            var zone = GetHostedZone(recordName);
            var subDomain = recordName.Substring(0, recordName.LastIndexOf(zone)).TrimEnd('.');
            _azureDnsClient.RecordSets.Delete(
                _options.ResourceGroupName,
                zone, 
                subDomain, 
                RecordType.TXT);
        }
    }
}
