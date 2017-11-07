using ACMESharp;
using Autofac;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Rest.Azure.Authentication;
using Nager.PublicSuffix;
using System;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class AzureFactory : IValidationPluginFactory
    {
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_DNS;
        public string Description => "Azure DNS";
        public string Name => nameof(Azure);
        public bool CanValidate(Target target) => true;
        public Type Instance => typeof(Azure);
    }

    class Azure : DnsValidation
    {
        private static DomainParser _domainParser = new DomainParser(new WebTldRuleProvider());
        private DnsManagementClient _DnsClient;
        private IOptionsService _options;
        private IInputService _input;

        public Azure(
            Target target, 
            ILogService logService, 
            IOptionsService optionsService,
            IInputService inputService) : base(logService)
        {
            // Build the service credentials and DNS management client
            var serviceCreds = ApplicationTokenProvider.LoginSilentAsync(
                target.DnsAzureOptions.TenantId,
                target.DnsAzureOptions.ClientId,
                target.DnsAzureOptions.Secret).Result;
            _DnsClient = new DnsManagementClient(serviceCreds) {
                SubscriptionId = target.DnsAzureOptions.SubscriptionId
            };
            _input = inputService;
            _options = optionsService;
            if (_domainParser == null)
            {
                _domainParser = new DomainParser(new WebTldRuleProvider());
            }
        }

        public override IValidationPlugin CreateInstance(Target target)
        {
            return new Azure(target);
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
