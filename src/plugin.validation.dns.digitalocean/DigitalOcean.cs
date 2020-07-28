using System;
using System.Threading.Tasks;
using DigitalOcean.API;
using DigitalOcean.API.Models.Requests;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class DigitalOcean : DnsValidation<DigitalOcean>
    {
        private readonly IDigitalOceanClient _doClient;
        private long? _recordId;

        public DigitalOcean(DigitalOceanOptions options, LookupClientProvider dnsClient, ILogService log, ISettingsService settings) : base(dnsClient, log, settings)
        {
            _doClient = new DigitalOceanClient(options.ApiToken.Value);
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                var (_, zone) = SplitDomain(record.Authority.Domain);
                if (_recordId == null)
                {
                    _log.Warning("Not deleting DNS records on DigitalOcean because of missing record id.");
                    return;
                }
                
                await _doClient.DomainRecords.Delete(zone, _recordId.Value);
                _log.Information("Successfully deleted DNS record on DigitalOcean.");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to delete DNS record on DigitalOcean.");
            }
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var (name, zone) = SplitDomain(record.Authority.Domain);
                var createdRecord = await _doClient.DomainRecords.Create(zone, new DomainRecord
                {
                    Type = "TXT",
                    Name = name,
                    Data = record.Value,
                    Ttl = 300
                });
                _recordId = createdRecord.Id;
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to create DNS record on DigitalOcean.");
                return false;
            }
        }

        private (string, string) SplitDomain(string domain)
        {
            var index = domain.IndexOf('.');
            return (domain.Substring(0, index), domain.Substring(index + 1));
        }
    }
}
