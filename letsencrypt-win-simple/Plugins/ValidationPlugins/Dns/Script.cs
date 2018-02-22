using ACMESharp;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// 'Universal' DNS validation with user-provided scripts
    /// </summary>
    class ScriptFactory : BaseValidationPluginFactory<DnsScript>
    {

        public ScriptFactory(ILogService log) : 
            base(log, 
                nameof(DnsScript), 
                "Run external program/script to create and update records", 
                AcmeProtocol.CHALLENGE_TYPE_DNS) { }

        /// <summary>
        /// This plugin was renamed due to a command line parser bug
        /// The following function ensured compatibility
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override bool Match(string name)
        {
            return base.Match(name) || string.Equals(name, "script", System.StringComparison.InvariantCultureIgnoreCase);
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            target.DnsScriptOptions = new DnsScriptOptions(optionsService, inputService, _log);
        }

        public override void Default(Target target, IOptionsService optionsService)
        {
            target.DnsScriptOptions = new DnsScriptOptions(optionsService, _log);
        }
    }

    class DnsScript : BaseDnsValidation
    {
        private DnsScriptOptions _dnsScriptOptions;
        private ScriptClient _scriptClient;

        public DnsScript(Target target, ILogService log, string identifier) : base(log, identifier)
        {
            _dnsScriptOptions = target.DnsScriptOptions;
            _scriptClient = new ScriptClient(log);
        }

        public override void CreateRecord(string identifier, string recordName, string token)
        {
            _scriptClient.RunScript(
                _dnsScriptOptions.CreateScript, 
                "{0} {1} {2}", 
                identifier, 
                recordName, 
                token);
        }

        public override void DeleteRecord(string identifier, string recordName)
        {
            _scriptClient.RunScript(
                _dnsScriptOptions.DeleteScript,
                "{0} {1}",
                identifier,
                recordName);
        }
    }
}
