using ARSoft.Tools.Net;
using ARSoft.Tools.Net.Dns;
using ARSoft.Tools.Net.Dns.DynamicUpdate;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
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
        private readonly LookupClientProvider _lookupClientProvider;
        private readonly DomainParseService _domainParser;
        private ArDnsClient? _client;

        public Rfc2136(
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings,
            SecretServiceManager ssm,
            LookupClientProvider lcp,
            DomainParseService domainParser,
            Rfc2136Options options): base(dnsClient, log, settings)
        {
            _options = options;
            _lookupClientProvider = lcp;
            _domainParser = domainParser;
            var key = ssm.EvaluateSecret(options.TsigKeySecret);
            if (string.IsNullOrEmpty(key))
            {
                throw new InvalidOperationException("Missing TsigKeySecret");
            }
            _key = key;
        }

        /// <summary>
        /// Allow multiple validation at once when DisableMultiThreading = false
        /// </summary>
        public override ParallelOperations Parallelism => ParallelOperations.Answer;

        /// <summary>
        /// Create record using AddRecordUpdate message
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            var zone = _domainParser.GetRegisterableDomain(record.Context.Identifier);
            var msg = new DnsUpdateMessage { ZoneName = DomainName.Parse(zone) };
            msg.Updates.Add(
                new AddRecordUpdate(
                    new TxtRecord(
                        DomainName.Parse(record.Authority.Domain),
                        60,
                        record.Value)));
            try
            {
                _log.Verbose("Creating record {name} in zone {zone}", record.Authority.Domain, zone);
                await SendUpdate(msg);
                return true;
            } 
            catch (Exception ex)
            {
                _log.Error(ex, "Error creating DNS record");
                return false;
            }
        }

        /// <summary>
        /// Delete record using DeleteRecordUpdate
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            var zone = _domainParser.GetRegisterableDomain(record.Context.Identifier);
            var msg = new DnsUpdateMessage { ZoneName = DomainName.Parse(zone) };
            var txtRecord = new TxtRecord(DomainName.Parse(record.Authority.Domain), 0, record.Value);

            // Work around bug https://github.com/alexreinert/ARSoft.Tools.Net/issues/28
            _ = typeof(TxtRecord).InvokeMember(
                nameof(txtRecord.RecordClass),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance,
                null,
                txtRecord,
                new object[] { RecordClass.None });
            var delete = new DeleteRecordUpdate(txtRecord);
            msg.Updates.Add(delete);
            try
            {
                _log.Verbose("Deleting record {name} in zone {zone}", record.Authority.Domain, zone);
                await SendUpdate(msg);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error deleting DNS record");
            }
        }

        /// <summary>
        /// Construct client and cache result
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        private async Task<ArDnsClient> GetClient()
        {
            if (_client == null)
            {
                var port = _options.ServerPort ?? 53;
                if (!IPAddress.TryParse(_options.ServerHost, out var ipAddress))
                {
                    if (string.IsNullOrEmpty(_options.ServerHost))
                    {
                        throw new InvalidOperationException("Missing ServerHost");
                    }
                    var lookup = await _lookupClientProvider.
                        GetSystemClient().
                        GetIps(_options.ServerHost);
                    if (!lookup.Any())
                    {
                        throw new Exception($"Unable to find IP for {_options.ServerHost}");
                    }
                    ipAddress = lookup.First();
                }
                _log.Verbose("Connnecting to DNS server at {ipAddress}:{port} using key {key}", ipAddress, port, _options.TsigKeyName);
                _client = new ArDnsClient(ipAddress, port);
            }
            return _client;
        }

        /// <summary>
        /// Add TSIG options and send the message to the server
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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

            var client = await GetClient();
            _log.Verbose("Send DNS update transaction {TransactionID}");
            var ret = await client.SendUpdateAsync(msg);
            if (ret == null || ret.ReturnCode != ReturnCode.NoError)
            {
                throw new Exception(ret?.ReturnCode.ToString() ?? "no response");
            }
        }
    }
}