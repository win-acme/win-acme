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
    class AzureFactory : BaseValidationPluginFactory<DnsScript>
    {
        public AzureFactory(ILogService log) : base(log, nameof(Azure), "Azure DNS", AcmeProtocol.CHALLENGE_TYPE_DNS){ }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
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
        private static DomainParser _domainParser = new DomainParser(new WebTldRuleProvider());
        private DnsManagementClient _DnsClient;
        private AzureDnsOptions _AzureDnsOptions;

        public Azure(ScheduledRenewal target, ILogService logService) : base(logService)
        {
            _AzureDnsOptions = target.Binding.DnsAzureOptions;

            // Build the service credentials and DNS management client
            var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(
                _AzureDnsOptions.TenantId,
                _AzureDnsOptions.ClientId,
                _AzureDnsOptions.Secret).Result;
            _DnsClient = new DnsManagementClient(serviceCreds) {
                SubscriptionId = _AzureDnsOptions.SubscriptionId
            };
            if (_domainParser == null)
            {
                _domainParser = new DomainParser(new WebTldRuleProvider());
            }
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

            _DnsClient.RecordSets.CreateOrUpdate(_AzureDnsOptions.ResourceGroupName, 
                url.RegistrableDomain,
                url.SubDomain,
                RecordType.TXT, 
                recordSetParams);
        }

        public override void DeleteRecord(string identifier, string recordName)
        {
            var url = _domainParser.Get(recordName);
            _DnsClient.RecordSets.Delete(_AzureDnsOptions.ResourceGroupName, url.RegistrableDomain, url.SubDomain, RecordType.TXT);
        }
    }
}
