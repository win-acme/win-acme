using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [IPlugin.Plugin<AcmeOptions, AcmeOptionsFactory, WacsJsonPlugins>
        ("c13acc1b-7571-432b-9652-7a68a5f506c5", "acme-dns", "Create verification records with acme-dns (https://github.com/joohoi/acme-dns)")]
    internal class Acme : DnsValidation<Acme>
    {
        private readonly IInputService _input;
        private readonly IProxyService _proxy;
        private readonly AcmeOptions _options;

        public Acme(
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings,
            IInputService input,
            IProxyService proxy,
            AcmeOptions options) :
            base(dnsClient, log, settings)
        {
            _options = options;
            _input = input;
            _proxy = proxy;
        }

        /// <summary>
        /// Send API call to the acme-dns server
        /// </summary>
        /// <param name="recordName"></param>
        /// <param name="token"></param>
        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            var client = new AcmeDnsClient(_dnsClient, _proxy, _log, _settings, _input, new Uri(_options.BaseUri!));
            return await client.Update(record.Context.Identifier, record.Value);
        }

        public override Task DeleteRecord(DnsValidationRecord record) => Task.CompletedTask;
    }
}
