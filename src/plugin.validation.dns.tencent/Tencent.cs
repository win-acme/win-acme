using Newtonsoft.Json.Linq;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using TencentCloud.Common;
using TencentCloud.Common.Profile;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [IPlugin.Plugin<
        TencentOptions, TencentOptionsFactory,
        DnsValidationCapability, TencentJson>
        ("6ea628c3-0f74-68bb-cf17-4fdd3d53f3af",
        "Tencent", "Create verification records in Tencent DNS")]
    public class Tencent : DnsValidation<Tencent>, IDisposable
    {
        private TencentOptions _options { get; }
        private SecretServiceManager _ssm { get; }
        private HttpClient _hc { get; }
        private Credential _cred { get; }

        public Tencent(
            TencentOptions options,
            SecretServiceManager ssm,
            IProxyService proxyService,
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings) : base(dnsClient, log, settings)
        {
            _options = options;
            _ssm = ssm;
            _hc = proxyService.GetHttpClient();
            //
            _cred = new Credential
            {
                SecretId = _ssm.EvaluateSecret(_options.ApiID),
                SecretKey = _ssm.EvaluateSecret(_options.ApiKey),
            };
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
                _log.Error($"Unable to add TencentDNS record: {ex.Message}");
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
                _log.Error($"Unable to delete TencentDNS record: {ex.Message}");
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
            var act = "CreateRecord";
            var client = GetCommonClient("DescribeRecordList");
            var param = new
            {
                Domain = domain,
                SubDomain = subDomain,
                RecordType = "TXT",
                RecordLine = "默认",
                Value = value,
            };
            var req = new CommonRequest(param);
            var resp = client.Call(req, act);
            //Console.WriteLine(resp);
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
            var act = "DeleteRecord";
            var client = GetCommonClient("DescribeRecordList");
            var param = new { Domain = domain, RecordId = recordId };
            var req = new CommonRequest(param);
            var resp = client.Call(req, act);
            //Console.WriteLine(resp);
            return true;
        }

        /// <summary>
        /// Get RecordID
        /// </summary>
        /// <param name="domain">Domain</param>
        /// <param name="subDomain">SubDomain</param>
        /// <returns></returns>
        private long GetRecordID(string domain, string subDomain)
        {
            var act = "DescribeRecordList";
            var client = GetCommonClient("DescribeRecordList");
            var param = new { Domain = domain };
            var req = new CommonRequest(param);
            var resp = client.Call(req, act);
            //Console.WriteLine(resp);
            //Anonymous Value
            var json = JObject.Parse(resp);
            var jsonData = json["Response"]!["RecordList"];
            var jsonDataLinq = jsonData!.Where(w => w["Name"]!.ToString() == subDomain && w["Type"]!.ToString() == "TXT");
            if (jsonDataLinq.Any()) return (long)jsonDataLinq.First()["RecordId"]!;
            return default;
        }

        /// <summary>
        /// DnsPod Server
        /// </summary>
        private const string DnsPodServer = "dnspod.tencentcloudapi.com";

        /// <summary>
        /// Get CommonClient
        /// </summary>
        /// <param name="modTemp">Mod</param>
        /// <param name="verTemp">Ver</param>
        /// <param name="regionTemp">Region</param>
        /// <param name="endpointTemp">DnsPodServer</param>
        /// <returns></returns>
        private CommonClient GetCommonClient(string? modTemp = default, string? verTemp = default, string? regionTemp = default, string? endpointTemp = default)
        {
            var mod = modTemp ?? "dnspod";
            var ver = verTemp ?? "2021-03-23";
            var region = regionTemp ?? "";
            var hpf = new HttpProfile
            {
                ReqMethod = "POST",
                Endpoint = endpointTemp ?? DnsPodServer,
            };
            var cpf = new ClientProfile(ClientProfile.SIGN_TC3SHA256, hpf);
            var client = new CommonClient(mod, ver, _cred, region, cpf);
            return client;
        }

        #endregion PrivateLogic

        public void Dispose() => _hc.Dispose();
    }
}
