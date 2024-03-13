using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns.Models;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [IPlugin.Plugin<
        HetznerOptions, HetznerOptionsFactory,
        DnsValidationCapability, HetznerJson>
        ("7176cc8f-ba08-4b07-aa39-2f5d012c1d5a",
        "Hetzner", "Create verification records in Hetzner DNS")]
    public class Hetzner : DnsValidation<Hetzner>, IDisposable
    {
        private readonly HetznerOptions _options;
        private readonly HetznerClient _client;

        public Hetzner(
            HetznerOptions options,
            IProxyService proxyService,
            LookupClientProvider dnsClient,
            SecretServiceManager ssm,
            ILogService logService,
            ISettingsService settings) : base(dnsClient, logService, settings)
        {
            _client = new HetznerClient(ssm.EvaluateSecret(options.ApiToken) ?? throw new InvalidOperationException("API Token cannot be null"), logService, proxyService);
            _options = options;
        }

        private async Task<Zone?> GetHostedZone(string recordName)
        {
            if (String.IsNullOrWhiteSpace(_options.ZoneId) is false)
            {
                _log.Debug("Using Zone Id specified by input arguments to get zone information.");

                return await _client.GetZoneAsync(_options.ZoneId).ConfigureAwait(false);
            }

            _log.Debug($"Try getting best matching zone for record '{recordName}'.");

            var zones = await _client.GetAllZonesAsync().ConfigureAwait(false);
            if (zones.Count == 0)
            {
                _log.Error("No zones could be found using the Hetzner DNS API. " +
                    "Maybe you entered a wrong API Token?");
                throw new Exception();
            }

            var bestZone = FindBestMatch(zones.Where(x => x.Paused is false).ToDictionary(x => x.Name), recordName);
            if (bestZone == null)
            {
                _log.Error($"No zone could be found that matches with record {recordName} and is not paused. " +
                    $"Maybe the API Token does not allow access to your domain?");
                throw new Exception();
            }

            _log.Information($"Best matching zone found: {bestZone.Name} - {bestZone.Status}");

            return bestZone;
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            var zone = await GetHostedZone(record.Authority.Domain).ConfigureAwait(false);
            if (zone == null)
            {
                _log.Error("The zone could not be found using the Hetzner DNS API, thus creating a DNS validation record is impossible. " +
                    $"Please note you need to use an API Token, not the Global API Key. The token needs appropriate  permissions. " +
                    $"It is important, that this token has access to all zones in your account because we need to " +
                    $"list your zones. Read the docs carefully for instructions.");
                return false;
            }

            var host = record.Authority.Domain.Replace($".{zone.Name}", null);
            var txtRecord = new Record("TXT", host, record.Value, zone.Id);

            return await _client.CreateRecordAsync(txtRecord).ConfigureAwait(false);
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                var zone = await GetHostedZone(record.Authority.Domain).ConfigureAwait(false);
                if (zone == null)
                {
                    _log.Error("The zone could not be found using the Hetzner DNS API, thus creating a DNS validation record is impossible. " +
                        $"Please note you need to use an API Token, not the Global API Key. The token needs appropriate  permissions. " +
                        $"It is important, that this token has access to all zones in your account because we need to " +
                        $"list your zones. Read the docs carefully for instructions.");
                    return;
                }

                var host = record.Authority.Domain.Replace($".{zone.Name}", null);
                var txtRecord = new Record("TXT", host, record.Value, zone.Id);

                await _client.DeleteRecordAsync(txtRecord).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Hetzner DNS: {ex.Message}");
            }
        }

        public void Dispose() => _client.Dispose();
    }
}
