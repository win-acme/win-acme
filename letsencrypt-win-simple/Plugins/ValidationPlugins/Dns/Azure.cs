using ACMESharp;
using LetsEncrypt.ACME.Simple.Plugins.Base;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using Nager.PublicSuffix;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Azure DNS validation
    /// </summary>
    class AzureFactory : BaseValidationPluginFactory<Azure>
    {
        public AzureFactory(ILogService log) : base(log, nameof(Azure), "Azure DNS", AcmeProtocol.CHALLENGE_TYPE_DNS){ }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            target.DnsAzureOptions = new AzureDnsOptions(optionsService, inputService);
        }

        public override void Default(Target target, IOptionsService optionsService)
        {
            target.DnsAzureOptions = new AzureDnsOptions(optionsService);
        }
    }

    class Azure : BaseDnsValidation
    {
        private DnsManagementClient _dnsClient;
        private AzureDnsOptions _azureDnsOptions;
        private DomainParser _domainParser;

        public Azure(Target target, DomainParser domainParser, ILogService log, string identifier) : base(log, identifier)
        {
            _azureDnsOptions = target.DnsAzureOptions;
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
