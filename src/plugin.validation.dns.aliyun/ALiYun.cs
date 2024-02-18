using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Alidns20150109.Models;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [IPlugin.Plugin<
        ALiYunOptions, ALiYunOptionsFactory,
        DnsValidationCapability, ALiYunJson>
        ("1d4db2ea-ce7c-46ce-b86f-40b356fcf999",
        "ALiYun", "Create verification records in ALiYun DNS")]
    public class ALiYun : DnsValidation<ALiYun>, IDisposable
    {
        private ALiYunOptions _options { get; }
        private SecretServiceManager _ssm { get; }
        private HttpClient _hc { get; }
        private AlibabaCloud.SDK.Alidns20150109.Client _client { get; }

        public ALiYun(
            ALiYunOptions options,
            SecretServiceManager ssm,
            IProxyService proxyService,
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings) : base(dnsClient, log, settings)
        {
            _options = options;
            _ssm = ssm;
            _hc = proxyService.GetHttpClient();
            //New Client
            var config = new Config
            {
                AccessKeyId = _ssm.EvaluateSecret(_options.ApiID),
                AccessKeySecret = _ssm.EvaluateSecret(_options.ApiSecret),
                Endpoint = _ssm.EvaluateSecret(_options.ApiServer),
            };
            _client = new AlibabaCloud.SDK.Alidns20150109.Client(config);
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            await Task.Delay(0);
            try
            {
                var identifier = record.Context.Identifier;
                var domain = record.Authority.Domain;
                var value = record.Value;
                //Add Record
                return AddRecord(identifier, domain, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //Out Error
                _log.Error($"Unable to add ALiYunDNS record: {ex.Message}");
            }
            return false;
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            await Task.Delay(0);
            try
            {
                var identifier = record.Context.Identifier;
                var domain = record.Authority.Domain;
                //Delete Record
                DelRecord(identifier, domain);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                //Out Error
                _log.Error($"Unable to delete ALiYunDNS record: {ex.Message}");
            }
        }

        #region PrivateLogic

        /// <summary>
        /// Add Record
        /// </summary>
        /// <param name="domain">Domain</param>
        /// <param name="subDomain">SubDomain</param>
        /// <param name="value">Value</param>
        /// <returns></returns>
        private bool AddRecord(string domain, string subDomain, string value)
        {
            subDomain = subDomain.Replace($".{domain}", "");
            //Delete Record
            DelRecord(domain, subDomain);
            //Add Record
            var addRecords = new AddDomainRecordRequest
            {
                DomainName = domain,
                RR = subDomain,
                Type = "TXT",
                Value = value
            };
            var runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            var data = _client.AddDomainRecordWithOptions(addRecords, runtime);
            //Console.WriteLine(data);
            return true;
        }

        /// <summary>
        /// Delete Record
        /// </summary>
        /// <param name="domain">Domain</param>
        /// <param name="subDomain">SubDomain</param>
        /// <returns></returns>
        private bool DelRecord(string domain, string subDomain)
        {
            subDomain = subDomain.Replace($".{domain}", "");
            //Get RecordID
            var recordId = GetRecordID(domain, subDomain);
            if (recordId == default) return false;
            //Delete Record
            var delRecords = new DeleteDomainRecordRequest
            {
                RecordId = recordId.ToString(),
            };
            var runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            var data = _client.DeleteDomainRecordWithOptions(delRecords, runtime);
            //Console.WriteLine(data);
            return true;
        }

        /// <summary>
        /// Get RecordID
        /// </summary>
        /// <param name="domain">Domain</param>
        /// <param name="subDomain">SubDomain</param>
        /// <returns></returns>
        private string? GetRecordID(string domain, string subDomain)
        {
            var getRecords = new DescribeDomainRecordsRequest
            {
                DomainName = domain,
            };
            var runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            var data = _client.DescribeDomainRecordsWithOptions(getRecords, runtime);
            //Console.WriteLine(data);
            var jsonDataLinq = data.Body.DomainRecords.Record.Where(w => w.RR == subDomain && w.Type == "TXT");
            if (jsonDataLinq.Any()) return jsonDataLinq.First().RecordId;
            return default;
        }

        #endregion PrivateLogic

        public void Dispose() => _hc.Dispose();
    }
}
