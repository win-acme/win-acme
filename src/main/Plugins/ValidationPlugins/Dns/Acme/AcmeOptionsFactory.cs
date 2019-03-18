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

        public AcmeOptionsFactory(
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings,
            ProxyService proxy) :
            base(log, Constants.Dns01ChallengeType)
        {
            _proxy = proxy;
            _settings = settings;
            _dnsClient = dnsClient;
        }

        public override AcmeOptions Aquire(Target target, IArgumentsService arguments, IInputService input, RunLevel runLevel)
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

        public override AcmeOptions Default(Target target, IArgumentsService arguments)
        {
            throw new NotSupportedException("Setting up acme-dns is not supported in unattended mode because it requires manual steps, specifically creating the CNAME record.");
        }

        public override bool CanValidate(Target target)
        {
            return true;
        }
    }
}
