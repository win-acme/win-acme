using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using TransIp.Library;
using TransIp.Library.Dto;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [IPlugin.Plugin<
        TransIpOptions, TransIpOptionsFactory, 
        DnsValidationCapability, TransIpJson>
        ("c49a7a9a-f8c9-494a-a6a4-c6b9daae7d9d", 
        "TransIp", 
        "Create verification records at TransIp",
        ChallengeType = Constants.Dns01ChallengeType)]
    internal sealed class TransIp : DnsValidation<TransIp>
    {
        private readonly DnsService _dnsService;
        private readonly DomainParseService _domainParser;

        public TransIp(
            LookupClientProvider dnsClient,
            ILogService log,
            IProxyService proxy,
            ISettingsService settings,
            DomainParseService domainParser,
            SecretServiceManager ssm,
            TransIpOptions options) : base(dnsClient, log, settings)
        {
            var auth = new AuthenticationService(options.Login, ssm.EvaluateSecret(options.PrivateKey), proxy);
            _dnsService = new DnsService(auth, proxy);
            _domainParser = domainParser;
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var domain = _domainParser.GetRegisterableDomain(record.Authority.Domain);
                var recordName = RelativeRecordName(domain, record.Authority.Domain);
                await _dnsService.CreateDnsEntry(
                    domain,
                    new DnsEntry()
                    {
                        Content = record.Value,
                        Name = recordName,
                        Type = "TXT"
                    });
                return true;
            }
            catch (Exception ex)
            {
                _log.Warning($"Error creating TXT record: {ex.Message}");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                var domain = _domainParser.GetRegisterableDomain(record.Authority.Domain);
                var recordName = RelativeRecordName(domain, record.Authority.Domain);
                await _dnsService.DeleteDnsEntry(
                    domain,
                    new DnsEntry()
                    {
                        Content = record.Value,
                        Name = recordName,
                        Type = "TXT"
                    });
            }
            catch (Exception ex)
            {
                _log.Warning($"Error deleting TXT record: {ex.Message}");
            }
        }
    }
}