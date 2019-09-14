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
        private readonly string _identifier;

        internal const string DefaultCreateArguments = "create {Identifier} {RecordName} {Token}";
        internal const string DefaultDeleteArguments = "delete {Identifier} {RecordName} {Token}";

        public Script(
            ScriptOptions options,
            LookupClientProvider dnsClient,
            ILogService log,
            string identifier) :
            base(dnsClient, log)
        {
            _identifier = identifier;
            _options = options;
            _scriptClient = new ScriptClient(log);
        }

        public override async Task CreateRecord(string recordName, string token)
        {
            var script = _options.Script ?? _options.CreateScript;
            if (!string.IsNullOrWhiteSpace(script))
            {
                var args = DefaultCreateArguments;
                if (!string.IsNullOrWhiteSpace(_options.CreateScriptArguments))
                {
                    args = _options.CreateScriptArguments;
                }
                await _scriptClient.RunScript(script, ProcessArguments(recordName, token, args, script.EndsWith(".ps1")));
            }
            else
            {
                _log.Error("No create script configured");
            }
        }

        public override async Task DeleteRecord(string recordName, string token)
        {
            var script = _options.Script ?? _options.DeleteScript;
            if (!string.IsNullOrWhiteSpace(script))
            {
                var args = DefaultDeleteArguments;
                if (!string.IsNullOrWhiteSpace(_options.DeleteScriptArguments))
                {
                    args = _options.DeleteScriptArguments;
                }
                await _scriptClient.RunScript(script, ProcessArguments(recordName, token, args, script.EndsWith(".ps1")));
            }
            else
            {
                _log.Warning("No delete script configured, validation record remains");
            }
        }

        private string ProcessArguments(string recordName, string token, string args, bool escapeToken)
        {
            var ret = args;
            ret = ret.Replace("{Identifier}", _identifier);
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
