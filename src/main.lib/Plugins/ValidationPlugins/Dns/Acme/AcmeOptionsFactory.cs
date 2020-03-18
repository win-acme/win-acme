using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class AcmeOptionsFactory : ValidationPluginOptionsFactory<Acme, AcmeOptions>
    {
        private readonly ProxyService _proxy;
        private readonly ISettingsService _settings;
        private readonly LookupClientProvider _dnsClient;
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public AcmeOptionsFactory(
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings,
            ProxyService proxy,
            IArgumentsService arguments) : base(Constants.Dns01ChallengeType)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
            _proxy = proxy;
            _dnsClient = dnsClient;
        }

        public override async Task<AcmeOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var ret = new AcmeOptions();
            Uri? uri = null;
            while (ret.BaseUri == null)
            {
                try
                {
                    var userInput = await input.RequestString("URL of the acme-dns server");
                    uri = new Uri(userInput);
                    ret.BaseUri = uri.ToString();
                }
                catch { }
            }
            if (uri == null)
            {
                return null;
            }
            var acmeDnsClient = new AcmeDnsClient(_dnsClient, _proxy, _log, _settings, input, uri);
            var identifiers = target.Parts.SelectMany(x => x.Identifiers).Distinct();
            foreach (var identifier in identifiers)
            {
                var registrationResult = await acmeDnsClient.EnsureRegistration(identifier.Replace("*.", ""), true);
                if (!registrationResult)
                {
                    return null;
                }

            }
            return ret;
        }

        public override async Task<AcmeOptions?> Default(Target target)
        {
            Uri? baseUri = null;
            try
            {
                var baseUriRaw =
                    _arguments.TryGetRequiredArgument(nameof(AcmeArguments.AcmeDnsServer),
                    _arguments.GetArguments<AcmeArguments>().AcmeDnsServer);
                if (!string.IsNullOrEmpty(baseUriRaw))
                {
                    baseUri = new Uri(baseUriRaw);
                }
            }
            catch { }
            if (baseUri == null)
            {
                return null;
            }

            var ret = new AcmeOptions
            {
                BaseUri = baseUri.ToString()
            };
            var acmeDnsClient = new AcmeDnsClient(_dnsClient, _proxy, _log, _settings, null, baseUri);
            var identifiers = target.Parts.SelectMany(x => x.Identifiers).Distinct();
            var valid = true;
            foreach (var identifier in identifiers)
            {
                if (!await acmeDnsClient.EnsureRegistration(identifier.Replace("*.", ""), false))
                {
                    _log.Warning("No (valid) acme-dns registration could be found for {identifier}.", identifier);
                    valid = false;
                }
            }
            if (!valid)
            {
                _log.Warning($"Creating his renewal might fail because the acme-dns configuration for one or more identifiers looks unhealthy.");
            }
            return ret;
        }

        public override bool CanValidate(Target target) => true;
    }
}
