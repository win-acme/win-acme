using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;

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
                _options.Secret).Result;
            _azureDnsClient = new DnsManagementClient(serviceCreds) { SubscriptionId = _options.SubscriptionId };
        }

        public override void CreateRecord(string recordName, string token)
        {
            var url = _dnsClientProvider.DomainParser.Get(recordName);

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
                url.RegistrableDomain,
                url.SubDomain,
                RecordType.TXT, 
                recordSetParams);
        }

        public override void DeleteRecord(string recordName, string token)
        {
            var url = _dnsClientProvider.DomainParser.Get(recordName);
            _azureDnsClient.RecordSets.Delete(_options.ResourceGroupName, url.RegistrableDomain, url.SubDomain, RecordType.TXT);
        }
    }
}
