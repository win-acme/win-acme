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
                args = args.Replace("{Identifier}", _identifier);
                args = args.Replace("{RecordName}", recordName);
                args = args.Replace("{Token}", token);
                _scriptClient.RunScript(_options.Script ?? _options.CreateScript, args);
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
                args = args.Replace("{Identifier}", _identifier);
                args = args.Replace("{RecordName}", recordName);
                args = args.Replace("{Token}", token);
                _scriptClient.RunScript(_options.Script ?? _options.DeleteScript, args);
            }
            else
            {
                _log.Warning("No delete script configured, validation record remains");
            }
        }
    }
}
