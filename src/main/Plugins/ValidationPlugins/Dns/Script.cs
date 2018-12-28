using ACMESharp.Authorizations;
using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Script : BaseDnsValidation<ScriptOptions, Script>
    {
        private ScriptClient _scriptClient;

        public Script(Target target, ScriptOptions options, ILogService log, string identifier) : base(log, options, identifier)
        {
            _scriptClient = new ScriptClient(log);
        }

        public override void CreateRecord(string identifier, string recordName, string token)
        {
            _scriptClient.RunScript(
                _options.ScriptConfiguration.CreateScript, 
                "create {0} {1} {2}", 
                identifier, 
                recordName, 
                token);
        }

        public override void DeleteRecord(string identifier, string recordName)
        {
            _scriptClient.RunScript(
                _options.ScriptConfiguration.DeleteScript,
                "delete {0} {1}",
                identifier,
                recordName);
        }
    }
}
