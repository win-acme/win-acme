using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class Script : DnsValidation
    {
        private DnsScriptOptions _dnsScriptOptions;
        private ScriptClient _scriptClient;

        public Script() { }
        public Script(Target target)
        {
            _dnsScriptOptions = target.DnsScriptOptions;
            _scriptClient = new ScriptClient();
        }

        public override string Name => nameof(Script);
        public override string Description => "Run external program/script to create and update records";

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

        public override void Aquire(IOptionsService options, InputService input, Target target)
        {
            target.DnsScriptOptions = new DnsScriptOptions(options, input);
        }

        public override void Default(IOptionsService options, Target target)
        {
            target.DnsScriptOptions = new DnsScriptOptions(options);
        }

        public override IValidationPlugin CreateInstance(Target target)
        {
            return new Script(target);
        }
    }
}
