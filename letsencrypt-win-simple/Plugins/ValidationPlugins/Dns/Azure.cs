using ACMESharp.Authorizations;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using Nager.PublicSuffix;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Azure : BaseDnsValidation<AzureOptions, Azure>
    {
        private DnsManagementClient _dnsClient;
        private AzureDnsOptions _azureDnsOptions;
        private DomainParser _domainParser;

        public Azure(Target target, AzureOptions options, DomainParser domainParser, ILogService log, string identifier) : base(log, options, identifier)
        {
            _azureDnsOptions = _options.AzureConfiguration;
            _domainParser = domainParser;

            // Build the service credentials and DNS management client
            var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(
                _azureDnsOptions.TenantId,
                _azureDnsOptions.ClientId,
                _azureDnsOptions.Secret).Result;
            _dnsClient = new DnsManagementClient(serviceCreds) { SubscriptionId = _azureDnsOptions.SubscriptionId };
        }

        public override void CreateRecord(string identifier, string recordName, string token)
        {
            var url = _domainParser.Get(recordName);

            // Create record set parameters
            var recordSetParams = new RecordSet
            {
                TTL = 3600,
                TxtRecords = new List<TxtRecord>
                {
                    new TxtRecord(new[] { token })
                }
            };

            _dnsClient.RecordSets.CreateOrUpdate(_azureDnsOptions.ResourceGroupName, 
                url.RegistrableDomain,
                url.SubDomain,
                RecordType.TXT, 
                recordSetParams);
        }

        public override void DeleteRecord(string identifier, string recordName)
        {
            var url = _domainParser.Get(recordName);
            _dnsClient.RecordSets.Delete(_azureDnsOptions.ResourceGroupName, url.RegistrableDomain, url.SubDomain, RecordType.TXT);
        }
    }
}
