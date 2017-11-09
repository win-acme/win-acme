using ACMESharp;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class ScriptFactory : BaseValidationPluginFactory<Script>
    {
        public ScriptFactory() :
            base(nameof(Script),
             "Run external program/script to create and update records",
            AcmeProtocol.CHALLENGE_TYPE_DNS)
        { }
    }

    class Script : DnsValidation
    {
        private DnsScriptOptions _dnsScriptOptions;
        private ScriptClient _scriptClient;
        private IOptionsService _optionsService;
        private IInputService _inputService;

        public Script(
            ScheduledRenewal target,
            ILogService logService,
            IOptionsService optionsService,
            IInputService inputService) : base(logService)
        {
            _inputService = inputService;
            _optionsService = optionsService;
            _dnsScriptOptions = target.Binding.DnsScriptOptions;
            _scriptClient = new ScriptClient(logService);
        }

        public override void CreateRecord(Target target, string identifier, string recordName, string token)
        {
            _scriptClient.RunScript(
                _dnsScriptOptions.CreateScript, 
                "{0} {1} {2}", 
                identifier, 
                recordName, 
                token);
        }

        public override void DeleteRecord(Target target, string identifier, string recordName)
        {
            _scriptClient.RunScript(
                _dnsScriptOptions.DeleteScript,
                "{0} {1}",
                identifier,
                recordName);
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
        {
            target.DnsScriptOptions = new DnsScriptOptions(_optionsService, _inputService, _log);
        }

        public override void Default(Target target, IOptionsService optionsService)
        {
            target.DnsScriptOptions = new DnsScriptOptions(_optionsService, _log);
        }
    }
}
