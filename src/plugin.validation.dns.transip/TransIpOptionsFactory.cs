using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TransIp.Library;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class TransIpOptionsFactory : ValidationPluginOptionsFactory<TransIp, TransIpOptions>
    {
        private readonly ArgumentsInputService _arguments;
        private readonly ILogService _log;
        private readonly IProxyService _proxy;

        public TransIpOptionsFactory(
            ArgumentsInputService arguments,
            ILogService log,
            IProxyService proxy) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) 
        {
            _arguments = arguments;
            _log = log;
            _proxy = proxy;
        }

        private ArgumentResult<string> Login => _arguments.
            GetString<TransIpArguments>(a => a.Login).
            Required();

        private ArgumentResult<ProtectedString> PrivateKey => _arguments.
            GetProtectedString<TransIpArguments>(a => a.PrivateKey).
            Validate(x => Task.FromResult(CheckKey(x.Value)), "invalid private key").
            Required();

        public override async Task<TransIpOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            return new TransIpOptions()
            {
                Login = await Login.Interactive(input, "Username").GetValue(),
                PrivateKey = await PrivateKey.Interactive(input, "Private key", multiline: true).GetValue()
            };
        }

        public override async Task<TransIpOptions> Default(Target target)
        {
            var login = await Login.GetValue();

            var keyFile = await _arguments.
                GetString<TransIpArguments>(a => a.PrivateKeyFile).
                Validate(x => Task.FromResult(File.Exists(x)), "file doesn't exist").
                Validate(async x => CheckKey(await File.ReadAllTextAsync(x)), "invalid key").
                GetValue();

            var key = keyFile != null
                ? (await File.ReadAllTextAsync(keyFile)).Protect()
                : await PrivateKey.GetValue();

            return new TransIpOptions()
            {
                Login = login,
                PrivateKey = key
            };
        }

        private bool CheckKey(string privateKey)
        {
            try
            {
                _ = new AuthenticationService("", privateKey, _proxy);
                return true;
            }
            catch (Exception ex) 
            {
                _log.Error(ex, "Invalid private key");
            }
            return false;
        }

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
