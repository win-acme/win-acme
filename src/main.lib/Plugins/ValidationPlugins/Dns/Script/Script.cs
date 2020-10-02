using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Script : DnsValidation<Script>
    {
        private readonly ScriptClient _scriptClient;
        private readonly ScriptOptions _options;
        private readonly DomainParseService _domainParseService;
        internal const string DefaultCreateArguments = "create {Identifier} {RecordName} {Token}";
        internal const string DefaultDeleteArguments = "delete {Identifier} {RecordName} {Token}";

        public Script(
            ScriptOptions options,
            LookupClientProvider dnsClient,
            ScriptClient client,
            ILogService log,
            DomainParseService domainParseService,
            ISettingsService settings) :
            base(dnsClient, log, settings)
        {
            _options = options;
            _scriptClient = client;
            _domainParseService = domainParseService;
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            var script = _options.Script ?? _options.CreateScript;
            if (!string.IsNullOrWhiteSpace(script))
            {
                var args = DefaultCreateArguments;
                if (!string.IsNullOrWhiteSpace(_options.CreateScriptArguments))
                {
                    args = _options.CreateScriptArguments;
                }
                await _scriptClient.RunScript(
                    script, 
                    ProcessArguments(
                        record.Context.Identifier, 
                        record.Authority.Domain, 
                        record.Value,
                        args, 
                        script.EndsWith(".ps1")));
                return true;
            }
            else
            {
                _log.Error("No create script configured");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            var script = _options.Script ?? _options.DeleteScript;
            if (!string.IsNullOrWhiteSpace(script))
            {
                var args = DefaultDeleteArguments;
                if (!string.IsNullOrWhiteSpace(_options.DeleteScriptArguments))
                {
                    args = _options.DeleteScriptArguments;
                }
                await _scriptClient.RunScript(
                    script, 
                    ProcessArguments(
                        record.Context.Identifier,
                        record.Authority.Domain,
                        record.Value,
                        args, 
                        script.EndsWith(".ps1")));
            }
            else
            {
                _log.Warning("No delete script configured, validation record remains");
            }
        }

        private string ProcessArguments(string identifier, string recordName, string token, string args, bool escapeToken)
        {
            var ret = args;
            // recordName: _acme-challenge.sub.domain.com
            // zoneName: domain.com
            // nodeName: _acme-challenge.sub

            // recordName: domain.com
            // zoneName: domain.com
            // nodeName: @

            var zoneName = _domainParseService.GetRegisterableDomain(identifier);
            var nodeName = "@";
            if (recordName.Length > zoneName.Length)
            {
                // Offset by one to prevent trailing dot
                var idx = recordName.Length - zoneName.Length - 1;
                if (idx != 0)
                {
                    nodeName = recordName.Substring(0, idx);
                }
            }
            ret = ret.Replace("{ZoneName}", zoneName);
            ret = ret.Replace("{NodeName}", nodeName);
            ret = ret.Replace("{Identifier}", identifier);
            ret = ret.Replace("{RecordName}", recordName);
          
            // Some tokens start with - which confuses Powershell. We did not want to 
            // make a breaking change for .bat or .exe files, so instead escape the 
            // token with double quotes, as Powershell discards the quotes anyway and 
            // thus it's functionally equivalant.
            if (escapeToken && (ret.Contains(" {Token} ") || ret.EndsWith(" {Token}")))
            {
                ret.Replace("{Token}", "\"{Token}\"");
            }
            ret = ret.Replace("{Token}", token);
            return ret;
        }
    }
}
