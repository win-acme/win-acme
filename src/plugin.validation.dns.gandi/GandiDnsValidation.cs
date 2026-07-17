using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.Gandi;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [IPlugin.Plugin<
        GandiOptions, GandiOptionsFactory,
        DnsValidationCapability, GandiJson>
        ("3772b87a-33e5-4cee-befe-3c793b55b557",
        "Gandi", "Create verification records in Gandi LiveDNS")]
    internal class GandiDnsValidation : DnsValidation<GandiDnsValidation>
    {
        private readonly DnsManagementClient _client;

        public GandiDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            GandiOptions options,
            SecretServiceManager ssm,
            IProxyService proxyService)
            : base(dnsClient, logService, settings)
        {
            _client = new DnsManagementClient(
                ssm.EvaluateSecret(options.ApiToken) ?? "",
                logService, proxyService);
        }

        /// <summary>
        /// Find the best matching zone in the account for the given record name
        /// </summary>
        private async Task<string?> GetHostedZone(string recordName)
        {
            var domains = await _client.GetDomains();
            if (domains.Count == 0)
            {
                _log.Error("No domains could be found using the Gandi LiveDNS API. " +
                    "Maybe you entered a wrong API Token?");
                return null;
            }
            var bestZone = FindBestMatch(domains.ToDictionary(x => x), recordName);
            if (bestZone == null)
            {
                _log.Error($"No domain could be found that matches with record {recordName}. " +
                    $"Maybe the API Token does not allow access to your domain?");
            }
            return bestZone;
        }

        // Gandi LiveDNS stores TXT values including the surrounding double quotes
        private static string Quote(string value) => $"\"{value}\"";

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var zone = await GetHostedZone(record.Authority.Domain);
                if (zone == null)
                {
                    return false;
                }
                var recordName = RelativeRecordName(zone, record.Authority.Domain);
                var value = Quote(record.Value);

                // A single record set can hold multiple TXT values (e.g. for a
                // wildcard and its base domain), so merge with any existing ones
                var values = await _client.GetTxtValues(zone, recordName);
                if (!values.Contains(value))
                {
                    values.Add(value);
                }
                await _client.PutTxtValues(zone, recordName, values);
                return true;
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to create record at Gandi: {ex.Message}");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                var zone = await GetHostedZone(record.Authority.Domain);
                if (zone == null)
                {
                    return;
                }
                var recordName = RelativeRecordName(zone, record.Authority.Domain);
                var value = Quote(record.Value);

                // Only remove our own value; keep any other values that share the
                // same record set and drop the set entirely when nothing remains
                var values = await _client.GetTxtValues(zone, recordName);
                var remaining = values.Where(x => x != value).ToList();
                if (remaining.Count == 0)
                {
                    await _client.DeleteTxtRecord(zone, recordName);
                }
                else
                {
                    await _client.PutTxtValues(zone, recordName, remaining);
                }
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Gandi: {ex.Message}");
            }
        }
    }
}
