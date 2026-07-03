using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [IPlugin.Plugin<
        Level27Options, Level27OptionsFactory,
        DnsValidationCapability, Level27Json>
        ("e5b8d3f0-7c2a-4e91-b6d4-3a8f1c9e2d70",
        "Level27", "Create verification records in Level27 DNS")]
    internal class Level27 : DnsValidation<Level27>, IDisposable
    {
        private readonly Level27Client _client;
        private int? _domainId;
        private int? _recordId;

        public Level27(
            Level27Options options,
            IProxyService proxyService,
            LookupClientProvider dnsClient,
            SecretServiceManager ssm,
            ILogService log,
            ISettingsService settings) : base(dnsClient, log, settings)
            => _client = new Level27Client(
                ssm.EvaluateSecret(options.ApiKey) ?? throw new InvalidOperationException("Level27 API key cannot be null"),
                options.ApiUrl,
                log,
                proxyService);

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var domain = await _client.FindDomainAsync(record.Authority.Domain);
                if (domain == null)
                {
                    _log.Error("Unable to find a matching zone on Level27 for '{Domain}'.", record.Authority.Domain);
                    return false;
                }

                var name = RelativeRecordName(domain.Fullname, record.Authority.Domain);
                var recordId = await _client.CreateRecordAsync(domain.Id, name, record.Value);
                if (recordId == null)
                {
                    return false;
                }

                _domainId = domain.Id;
                _recordId = recordId;
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to create DNS record on Level27.");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                if (_domainId == null || _recordId == null)
                {
                    _log.Warning("Not deleting DNS record on Level27 because of missing record id.");
                    return;
                }

                await _client.DeleteRecordAsync(_domainId.Value, _recordId.Value);
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to delete record from Level27: {Message}", ex.Message);
            }
        }

        public void Dispose() => _client.Dispose();
    }
}
