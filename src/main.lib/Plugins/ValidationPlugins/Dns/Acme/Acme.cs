using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class Acme : DnsValidation<AcmeOptions, Acme>
    {
        private readonly ISettingsService _settings;
        private readonly IInputService _input;
        private readonly ProxyService _proxy;

        public Acme(
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings,
            IInputService input,
            ProxyService proxy,
            AcmeOptions options,
            string identifier) :
            base(dnsClient, log, options, identifier)
        {
            _settings = settings;
            _input = input;
            _proxy = proxy;
        }

        /// <summary>
        /// Send API call to the acme-dns server
        /// </summary>
        /// <param name="recordName"></param>
        /// <param name="token"></param>
        public override void CreateRecord(string recordName, string token)
        {
            var client = new AcmeDnsClient(_dnsClientProvider, _proxy, _log, _settings, _input, _options.BaseUri);
            client.Update(_identifier, token);
        }

        public override void DeleteRecord(string recordName, string token)
        {
            // Not supported, ignore the call
        }
    }
}
