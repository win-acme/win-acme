using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Script : DnsValidation<ScriptOptions, Script>
    {
        private ScriptClient _scriptClient;

        public Script(Target target, ScriptOptions options, ILogService log, string identifier) : base(log, options, identifier)
        {
            _scriptClient = new ScriptClient(log);
        }

        public override void CreateRecord(string recordName, string token)
        {
            _scriptClient.RunScript(
                _options.CreateScript, 
                "create {0} {1} {2}", 
                _identifier, 
                recordName, 
                token);
        }

        public override void DeleteRecord(string recordName)
        {
            _scriptClient.RunScript(
                _options.DeleteScript,
                "delete {0} {1}",
                _identifier,
                recordName);
        }
    }
}
