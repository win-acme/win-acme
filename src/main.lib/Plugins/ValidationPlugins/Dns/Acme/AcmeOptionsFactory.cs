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
        private readonly IProxyService _proxy;
        private readonly ISettingsService _settings;
        private readonly LookupClientProvider _dnsClient;
        private readonly ILogService _log;
        private readonly ArgumentsInputService _arguments;

        public AcmeOptionsFactory(
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings,
            IProxyService proxy,
            ArgumentsInputService arguments) : base(Constants.Dns01ChallengeType)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
            _proxy = proxy;
            _dnsClient = dnsClient;
        }

        private ArgumentResult<string?> Endpoint => _arguments.
            GetString<AcmeArguments>(x => x.AcmeDnsServer).
            Validate(x => Task.FromResult(new Uri(x!).ToString() != ""), "invalid uri").
            Required();

        public override async Task<AcmeOptions?> Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var ret = new AcmeOptions()
            {
                BaseUri = await Endpoint.Interactive(input).GetValue()
            };
            var acmeDnsClient = new AcmeDnsClient(_dnsClient, _proxy, _log, _settings, input, new Uri(ret.BaseUri!));
            var identifiers = target.Parts.SelectMany(x => x.Identifiers).Distinct();
            foreach (var identifier in identifiers)
            {
                var registrationResult = await acmeDnsClient.EnsureRegistration(identifier.Value.Replace("*.", ""), true);
                if (!registrationResult)
                {
                    return null;
                }
            }
            return ret;
        }

        public override async Task<AcmeOptions?> Default(Target target)
        {
            var ret = new AcmeOptions()
            {
                BaseUri = await Endpoint.GetValue()
            };
            var acmeDnsClient = new AcmeDnsClient(_dnsClient, _proxy, _log, _settings, null, new Uri(ret.BaseUri!));
            var identifiers = target.Parts.SelectMany(x => x.Identifiers).Distinct();
            var valid = true;
            foreach (var identifier in identifiers)
            {
                if (!await acmeDnsClient.EnsureRegistration(identifier.Value.Replace("*.", ""), false))
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

        public override bool CanValidate(Target target) => target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }
}
