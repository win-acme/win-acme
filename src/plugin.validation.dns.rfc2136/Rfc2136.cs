using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using ARSoft.Tools.Net.Dns.DynamicUpdate;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Net;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using ArDnsClient = ARSoft.Tools.Net.Dns.DnsClient;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [IPlugin.Plugin<
        Rfc2136Options, Rfc2136OptionsFactory, 
        DnsValidationCapability, Rfc2136Json>
        ("ed5dc9d1-739c-4f6a-854f-238bf65b63ee",
        "Rfc2136",
        "Create verification records using dynamic updates")]
    internal sealed class Rfc2136 : DnsValidation<Rfc2136>
    {
        private readonly string _key; 
        private readonly Rfc2136Options _options;
        private readonly ArDnsClient _client;

        public Rfc2136(
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings,
            SecretServiceManager ssm,
            Rfc2136Options options): base(dnsClient, log, settings)
        {
            _options = options;
            var key = ssm.EvaluateSecret(options.TsigKeySecret);
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Missing TsigKeySecret");
            }
            _key = key;

            if (string.IsNullOrEmpty(options.ServerHost)) 
            {
                throw new InvalidOperationException("Missing ServerHost");
            }
            _client = new ArDnsClient(
                IPAddress.Parse(options.ServerHost),
                options.ServerPort ?? 53);
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            var msg = new DnsUpdateMessage
            {
                ZoneName = DomainName.Parse(record.Context.Identifier)
            };
            msg.Updates.Add(
                new AddRecordUpdate(
                    new TxtRecord(
                        DomainName.Parse(record.Authority.Domain),
                        60,
                        record.Value)));
            try
            {
                await SendUpdate(msg);
                return true;
            } 
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating DNS record");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            var msg = new DnsUpdateMessage
            {
                ZoneName = DomainName.Parse(record.Context.Identifier)
            };
            msg.Updates.Add(
                new DeleteAllRecordsUpdate(
                    DomainName.Parse(record.Authority.Domain), 
                    RecordType.Txt));
            try
            {
                await SendUpdate(msg);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error deleting DNS record");
            }
        }

        private async Task SendUpdate(DnsUpdateMessage msg)
        {
            if (!Enum.TryParse<TSigAlgorithm>(_options.TsigKeyAlgorithm, true, out var algorithm)) 
            {
                algorithm = TSigAlgorithm.Md5;
            }

            msg.TSigOptions = new TSigRecord(
                DomainName.Parse(_options.TsigKeyName ?? ""),
                algorithm,
                DateTime.Now,
                new TimeSpan(0, 5, 0),
                msg.TransactionID,
                ReturnCode.NoError,
                null,
                Convert.FromBase64String(_key));

            var ret = await _client.SendUpdateAsync(msg);
            if (ret == null || ret.ReturnCode != ReturnCode.NoError)
            {
                throw new Exception(ret?.ReturnCode.ToString() ?? "no response");
            }
        }
    }
}