using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.ValidationPlugins.Simply;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{

    internal class SimplyDnsValidation : DnsValidation<SimplyDnsValidation>
    {
        private readonly SimplyDnsClient _client;

        public SimplyDnsValidation(
            LookupClientProvider dnsClient, 
            ILogService logService, 
            ISettingsService settings,
            SecretServiceManager ssm,
            SimplyOptions options)
            : base(dnsClient, logService, settings) 
            => _client = new SimplyDnsClient(options.Account ?? "", ssm.EvaluateSecret(options.ApiKey) ?? "", logService);

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                await _client.CreateRecordAsync(record.Context.Identifier, record.Authority.Domain, record.Value);
                return true;
            } 
            catch
            {
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                await _client.DeleteRecordAsync(record.Context.Identifier, record.Authority.Domain, record.Value);
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Simply: {ex.Message}");
            }
        }
    }
}
