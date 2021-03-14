using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;
using TransIp.Library;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class TransIpOptionsFactory : ValidationPluginOptionsFactory<TransIp, TransIpOptions>
    {
        private readonly IArgumentsService _arguments;
        private readonly ILogService _log;
        private readonly ProxyService _proxy;

        public TransIpOptionsFactory(IArgumentsService arguments, ILogService log, ProxyService proxy) : base(Dns01ChallengeValidationDetails.Dns01ChallengeType) 
        {
            _arguments = arguments;
            _log = log;
            _proxy = proxy;
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

        public override async Task<TransIpOptions> Default(Target target)
        {
            var args = _arguments.GetArguments<TransIpArguments>();
            var keyFile = args.PrivateKeyFile;
            var key = "";
            if (!string.IsNullOrEmpty(keyFile))
            {
                if (!File.Exists(keyFile))
                {
                    _log.Error("File {key} does not exist", keyFile);
                }
                else
                {
                    key = await File.ReadAllTextAsync(keyFile);
                }
            }
            if (string.IsNullOrEmpty(key))
            {
                key = _arguments.TryGetRequiredArgument("transip-privatekey", args.PrivateKey);
            }
            if (!CheckKey(key))
            {
                throw new Exception("Invalid key");
            }
            var ret = new TransIpOptions
            {
                PrivateKey = new ProtectedString(key),
                Login = _arguments.TryGetRequiredArgument("transip-login", args.Login)
            };
            return ret;
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

        public override bool CanValidate(Target target) => true;
    }
}
