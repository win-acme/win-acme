using Abstractions.Integrations.Domeneshop;

using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Options = Abstractions.Integrations.Domeneshop.DomeneshopOptions;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [IPlugin.Plugin<
        DomeneshopOptions, DomeneshopOptionsFactory,
        DnsValidationCapability, DomeneshopJson>
        ("0BD9B320-08E0-4BFE-A535-B979886187E4",
        "Domeneshop", "Create verification records in Domeneshop DNS",
        ChallengeType = Constants.Dns01ChallengeType)]
    internal class DomeneshopDnsValidation : DnsValidation<DomeneshopDnsValidation>
    {
        private readonly DomeneshopClient _client;
        private readonly DomainParseService _domainParser;
        private readonly ILogService _logService;

        private Domain? domain;
        private DnsRecord? txt;

        public DomeneshopDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            DomainParseService domainParser,
            DomeneshopOptions options,
            SecretServiceManager ssm)
            : base(dnsClient, logService, settings)
        {
            _client = new DomeneshopClient(new Options
            {
                ClientId = ssm.EvaluateSecret(options.ClientId) ?? "",
                ClientSecret = ssm.EvaluateSecret(options.ClientSecret) ?? ""
            });

            _domainParser = domainParser;
            _logService = logService;
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var domainname = _domainParser.GetRegisterableDomain(record.Authority.Domain);
                var recordName = RelativeRecordName(domainname, record.Authority.Domain);

                var domains = await _client.GetDomainsAsync();
                domain = domains.FirstOrDefault(d => d.Name.Equals(domainname, StringComparison.OrdinalIgnoreCase));

                if (domain == null)
                {
                    _logService.Error("The following domain could not be found as one of the users domains: {0}", domainname);
                    return false;
                }

                txt = new DnsRecord(DnsRecordType.TXT, recordName, record.Value);
                txt = await _client.EnsureDnsRecordAsync(domain.Id, txt);

                return true;
            }
            catch (Exception exception)
            {
                _logService.Error(exception, "Unhandled exception when attempting to create record");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                #pragma warning disable CS8602 // Dereference of a possibly null reference.
                #pragma warning disable CS8629 // Nullable value type may be null.
                await _client.DeleteDnsRecordAsync(domain.Id, txt.Id.Value);
                #pragma warning restore CS8629 // Nullable value type may be null.
                #pragma warning restore CS8602 // Dereference of a possibly null reference.

            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Domeneshop: {ex.Message}");
            }
        }

        public override Task Finalize()
        {
            _client?.Dispose();
            return base.Finalize();
        }
    }
}
