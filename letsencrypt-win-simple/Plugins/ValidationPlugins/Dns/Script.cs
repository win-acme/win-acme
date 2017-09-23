using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Dns
{
    class Script : DnsValidation
    {
        private DnsScriptOptions _dnsScriptOptions;

        public Script() { }
        public Script(Target target)
        {
            _dnsScriptOptions = target.DnsScriptOptions;
        }

        public override string Name => nameof(Script);
        public override string Description => "Run external program/script to create and update records";

        public override void CreateRecord(Target target, string identifier, string recordName, string token)
        {
            ScriptClient.RunScript(
                _dnsScriptOptions.CreateScript, 
                "{0} {1} {2}", 
                identifier, 
                recordName, 
                token);
        }

        public override void DeleteRecord(Target target, string identifier, string recordName)
        {
            ScriptClient.RunScript(
                _dnsScriptOptions.DeleteScript,
                "{0} {1}",
                identifier,
                recordName);
        }

        public override void Aquire(Options options, InputService input, Target target)
        {
            target.DnsScriptOptions = new DnsScriptOptions(options, input);
        }

        public override void Default(Options options, Target target)
        {
            target.DnsScriptOptions = new DnsScriptOptions(options);
        }

        public override IValidationPlugin CreateInstance(Target target)
        {
            return new Script(target);
        }
    }
}
