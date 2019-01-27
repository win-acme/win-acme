using PKISharp.WACS.Clients;
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

        public AcmeOptionsFactory(ILogService log, ISettingsService settings, ProxyService proxy) : 
            base(log, Constants.Dns01ChallengeType)
        {
            _proxy = proxy;
            _settings = settings;
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
            var acmeDnsClient = new AcmeDnsClient(_proxy, _log, _settings, input, ret.BaseUri);
            var identifiers = target.Parts.SelectMany(x => x.Identifiers).Distinct();
            foreach (var identifier in identifiers)
            {
                acmeDnsClient.EnsureRegistration(identifier.Replace("*.", ""));
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
