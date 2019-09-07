using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Net;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class AcmeOptionsFactory : ValidationPluginOptionsFactory<Acme, AcmeOptions>
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
            IArgumentsService arguments) :  base(Constants.Dns01ChallengeType)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
            _proxy = proxy;
            _dnsClient = dnsClient;
        }

        public override AcmeOptions Aquire(Target target, IInputService input, RunLevel runLevel)
        {
            var ret = new AcmeOptions();
            Uri baseUri = null;
            while (baseUri == null)
            {
                try
                {
                    baseUri = new Uri(input.RequestString("URL of the acme-dns server"));
                }
                catch { }
            }
            ret.BaseUri = baseUri.ToString();
            var acmeDnsClient = new AcmeDnsClient(_dnsClient, _proxy, _log, _settings, input, ret.BaseUri);
            var identifiers = target.Parts.SelectMany(x => x.Identifiers).Distinct();
            foreach (var identifier in identifiers)
            {
                if (!acmeDnsClient.EnsureRegistration(identifier.Replace("*.", ""), true))
                {
                    // Something failed or was aborted
                    return null;
                }
            }
            return ret;
        }

        public override AcmeOptions Default(Target target)
        {
            Uri baseUri = null;
            try
            {
                var baseUriRaw = 
                    _arguments.TryGetRequiredArgument(nameof(AcmeArguments.AcmeDnsServer),
                    _arguments.GetArguments<AcmeArguments>().AcmeDnsServer);
                if (!string.IsNullOrEmpty(baseUriRaw))
                {
                    baseUri = new Uri(baseUriRaw);
                }
            } catch {}
            if (baseUri == null)
            {
                return null;
            }

            var ret = new AcmeOptions
            {
                BaseUri = baseUri.ToString()
            };
            var acmeDnsClient = new AcmeDnsClient(_dnsClient, _proxy, _log, _settings, null, ret.BaseUri);
            var identifiers = target.Parts.SelectMany(x => x.Identifiers).Distinct();
            var valid = true;
            foreach (var identifier in identifiers)
            {
                if (!acmeDnsClient.EnsureRegistration(identifier.Replace("*.", ""), false))
                {
                    valid = false;
                }
            }
            if (!valid)
            {
                _log.Error($"Setting up this certificate is not possible in unattended mode because no (valid) acme-dns registration could be found for one or more of the specified domains.");
                return null;
            }
            return ret;
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
