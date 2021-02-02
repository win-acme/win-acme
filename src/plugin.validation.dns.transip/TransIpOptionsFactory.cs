using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Threading.Tasks;
using TransIp.Library;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class TransIpOptionsFactory : ValidationPluginOptionsFactory<TransIp, TransIpOptions>
    {
        private readonly IArgumentsService _arguments;
        private readonly ILogService _log;

        public TransIpOptionsFactory(IArgumentsService arguments, ILogService log) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) 
        {
            _arguments = arguments;
            _log = log;
        }

        public override async Task<TransIpOptions> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var args = _arguments.GetArguments<TransIpArguments>();
            var login = await _arguments.TryGetArgument(args.Login, input, "User name for the control panel");
            string key;
            do
            {
                key = await _arguments.TryGetArgument(args.PrivateKey, input, "Private key for the API, generated in the control panel", multiline: true);
            } while (!CheckKey(key));
            var options = new TransIpOptions()
            {
                Login = login,
                PrivateKey = new ProtectedString(key)
            };
            return options;
        }

        public override Task<TransIpOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<TransIpArguments>();
            var key = _arguments.TryGetRequiredArgument("transip-privatekey", args.PrivateKey);
            if (!CheckKey(key))
            {
                throw new Exception("Invalid key");
            }
            var ret = new TransIpOptions
            {
                PrivateKey = new ProtectedString(key),
                Login = _arguments.TryGetRequiredArgument("transip-login", args.Login)
            };
            return Task.FromResult(ret);
        }

        private bool CheckKey(string privateKey)
        {
            try
            {
                _ = new AuthenticationService("", privateKey, null);
                return true;
            }
            catch (Exception ex) 
            {
                _log.Error(ex, "Invalid private key");
            }
            return false;
        }

        public override bool CanValidate(Target target) => true;
    }
}
