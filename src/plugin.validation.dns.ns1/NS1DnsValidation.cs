using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns.NS1;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class NS1DnsValidation : DnsValidation<NS1DnsValidation>
    {
        private readonly DnsManagementClient _client;
        private static readonly Dictionary<string, string> _zonesMap = new Dictionary<string, string>();

        public NS1DnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            NS1Options options,
            SecretServiceManager ssm,
            IProxyService proxyService)
            : base(dnsClient, logService, settings)
        {
            _client = new DnsManagementClient(
                ssm.EvaluateSecret(options.ApiKey) ?? "",
                logService, proxyService);
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            var zones = await _client.GetZones();
            if (zones == null)
            {
                _log.Error("Failed to get DNS zones list for account. Aborting.");
                return false;
            }

            var zone = FindBestMatch(zones.ToDictionary(x => x), record.Authority.Domain);
            if (zone == null)
            {
                _log.Error("No matching zone found in NS1 account. Aborting");
                return false;
            }
            _zonesMap[record.Authority.Domain] = zone;

            var result = await _client.CreateRecord(zone, record.Authority.Domain, "TXT", record.Value);
            if (!result)
            {
                _log.Error("Failed to create DNS record. Aborting");
                return false;
            }

            return true;
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            string zone;
            if (!_zonesMap.TryGetValue(record.Authority.Domain, out zone!))
            {
                _log.Warning($"No record with name {record.Authority.Domain} was created");
                return;
            }
            _zonesMap.Remove(record.Authority.Domain);

            var result = await _client.DeleteRecord(zone, record.Authority.Domain, "TXT");
            if (!result)
            {
                _log.Error("Failed to delete DNS record");
                return;
            }
        }
    }
}
