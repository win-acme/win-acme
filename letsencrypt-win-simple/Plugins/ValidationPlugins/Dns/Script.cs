using ACMESharp;
using Autofac;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class ScriptFactory : IValidationPluginFactory
    {
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_DNS;
        public string Description => "Run external program/script to create and update records";
        public string Name => nameof(Script);
        public bool CanValidate(Target target) => true;
        public Type Instance => typeof(Script);
    }

    class Script : DnsValidation
    {
        private DnsScriptOptions _dnsScriptOptions;
        private ScriptClient _scriptClient;
        private IOptionsService _optionsService;
        private IInputService _inputService;

        public Script(
            Target target,
            ILogService logService,
            IOptionsService optionsService,
            IInputService inputService) : base(logService)
        {
            _inputService = inputService;
            _optionsService = optionsService;
            _dnsScriptOptions = target.DnsScriptOptions;
            _scriptClient = new ScriptClient();
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

        public override void Aquire(Target target)
        {
            target.DnsScriptOptions = new DnsScriptOptions(_optionsService, _inputService);
        }

        public override void Default(Target target)
        {
            target.DnsScriptOptions = new DnsScriptOptions(_optionsService);
        }
    }
}
