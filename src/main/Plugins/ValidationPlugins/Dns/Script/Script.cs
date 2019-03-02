using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Script : DnsValidation<ScriptOptions, Script>
    {
        private ScriptClient _scriptClient;

        internal const string DefaultCreateArguments = "create {Identifier} {RecordName} {Token}";
        internal const string DefaultDeleteArguments = "delete {Identifier} {RecordName} {Token}";

        public Script(Target target, ScriptOptions options, ILogService log, string identifier) : base(log, options, identifier)
        {
            _scriptClient = new ScriptClient(log);
        }

        public override void CreateRecord(string recordName, string token)
        {
            var script = _options.Script ?? _options.CreateScript;
            if (!string.IsNullOrWhiteSpace(script))
            {
                var args = DefaultCreateArguments;
                if (!string.IsNullOrWhiteSpace(_options.CreateScriptArguments))
                {
                    args = _options.CreateScriptArguments;
                }
                _scriptClient.RunScript(script, ProcessArguments(recordName, token, args, script.EndsWith(".ps1")));
            }
            else
            {
                _log.Error("No create script configured");
            }
        }

        public override void DeleteRecord(string recordName, string token)
        {
            var script = _options.Script ?? _options.DeleteScript;
            if (!string.IsNullOrWhiteSpace(script))
            {
                var args = DefaultDeleteArguments;
                if (!string.IsNullOrWhiteSpace(_options.DeleteScriptArguments))
                {
                    args = _options.DeleteScriptArguments;
                }    
                _scriptClient.RunScript(script, ProcessArguments(recordName, token, args, script.EndsWith(".ps1")));
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
            if (escapeToken && (ret.Contains(" {Token} ") || ret.EndsWith(" {Token}")))
            {
                ret.Replace("{Token}", "\"{Token}\"");
            }
            ret = ret.Replace("{Token}", token);
            return ret;
        }
    }
}
