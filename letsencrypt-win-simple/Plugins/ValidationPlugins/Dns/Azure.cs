using ACMESharp;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using Nager.PublicSuffix;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class AzureFactory : BaseValidationPluginFactory<Script>
    {
        public AzureFactory() :
            base(nameof(Azure),
            "Azure DNS",
            AcmeProtocol.CHALLENGE_TYPE_DNS){ }
    }

    class Azure : DnsValidation
    {
        private static DomainParser _domainParser = new DomainParser(new WebTldRuleProvider());
        private DnsManagementClient _DnsClient;
        private IOptionsService _options;
        private IInputService _input;

        public Azure(
            ScheduledRenewal target, 
            ILogService logService, 
            IOptionsService optionsService,
            IInputService inputService) : base(logService)
        {
            // Build the service credentials and DNS management client
            var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(
                target.Binding.DnsAzureOptions.TenantId,
                target.Binding.DnsAzureOptions.ClientId,
                target.Binding.DnsAzureOptions.Secret).Result;
            _DnsClient = new DnsManagementClient(serviceCreds) {
                SubscriptionId = target.Binding.DnsAzureOptions.SubscriptionId
            };
            _input = inputService;
            _options = optionsService;
            if (_domainParser == null)
            {
                _domainParser = new DomainParser(new WebTldRuleProvider());
            }
        }

        public override void CreateRecord(Target target, string identifier, string recordName, string token)
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

            _DnsClient.RecordSets.CreateOrUpdate(target.DnsAzureOptions.ResourceGroupName, 
                url.RegistrableDomain,
                url.SubDomain,
                RecordType.TXT, 
                recordSetParams);
        }

        public override void DeleteRecord(Target target, string identifier, string recordName)
        {
            var url = _domainParser.Get(recordName);
            _DnsClient.RecordSets.Delete(target.DnsAzureOptions.ResourceGroupName, url.RegistrableDomain, url.SubDomain, RecordType.TXT);
        }

        public override void Aquire(Target target)
        {
            target.DnsAzureOptions = new AzureDnsOptions(_options, _input);
        }

        public override void Default(Target target)
        {
            target.DnsAzureOptions = new AzureDnsOptions(_options);
        }
    }
}
