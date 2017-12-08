using ACMESharp;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins.Base;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
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

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
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

        public DnsScript(
            ScheduledRenewal target,
            ILogService logService) : base(logService)
        {
            _dnsScriptOptions = target.Binding.DnsScriptOptions;
            _scriptClient = new ScriptClient(logService);
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
