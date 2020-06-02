using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class Acme : DnsValidation<Acme>
    {
        private readonly IInputService _input;
        private readonly ProxyService _proxy;
        private readonly AcmeOptions _options;
        private readonly string _identifier;

        public Acme(
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings,
            IInputService input,
            ProxyService proxy,
            AcmeOptions options,
            string identifier) :
            base(dnsClient, log, settings)
        {
            _options = options;
            _identifier = identifier;
            _input = input;
            _proxy = proxy;
        }

        /// <summary>
        /// Send API call to the acme-dns server
        /// </summary>
        /// <param name="recordName"></param>
        /// <param name="token"></param>
        public override async Task<bool> CreateRecord(string recordName, string token)
        {
            var client = new AcmeDnsClient(_dnsClient, _proxy, _log, _settings, _input, new Uri(_options.BaseUri));
            return await client.Update(_identifier, token);
        }

        public override Task DeleteRecord(string recordName, string token) => Task.CompletedTask;
    }
}
