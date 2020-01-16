using FluentCloudflare.Abstractions.Builders;
using FluentCloudflare.Api;
using FluentCloudflare.Api.Entities;
using FluentCloudflare.Extensions;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class Cloudflare : DnsValidation<Cloudflare>
    {
        private readonly CloudflareOptions _options;
        private readonly DomainParseService _domainParser;
        private readonly HttpClient _hc;

        public Cloudflare(
            CloudflareOptions options,
            DomainParseService domainParser,
            ProxyService proxyService,
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings)
            : base(dnsClient, log, settings)
        {
            _options = options;
            _hc = proxyService.GetHttpClient();
            _domainParser = domainParser;
        }

        private IAuthorizedSyntax GetContext()
        {
            // avoid name collision with this class
            return FluentCloudflare.Cloudflare.WithToken(_options.ApiToken.Value);
        }

        private async Task<Zone> GetHostedZone(IAuthorizedSyntax context, string recordName)
        {
            var prs = _domainParser;
            var domainName = $"{prs.GetDomain(recordName)}.{prs.GetTLD(recordName)}";
            var zonesResp = await context.Zones.List()
                .WithName(domainName)
                .ParseAsync(_hc)
                .ConfigureAwait(false);

            if (!zonesResp.Success || (zonesResp.Result?.Count ?? 0) < 1)
            {
                _log.Error(
                    "Zone {domainName} could not be found using the Cloudflare API. Maybe you entered a wrong API Token or domain or the API Token does not allow access to this domain?",
                    domainName);
                // maybe throwing would be better
                // this is how the Azure DNS Validator works
                return null;
            }

            return zonesResp.Unpack().First();
        }

        public override async Task CreateRecord(string recordName, string token)
        {
            var ctx = GetContext();
            var zone = await GetHostedZone(ctx, recordName).ConfigureAwait(false);
            if (zone == null)
            {
                throw new InvalidOperationException($"The zone could not be found using the Cloudflare API, thus creating a DNS validation record is impossible.");
            }

            var dns = ctx.Zone(zone).Dns;
            await dns.Create(DnsRecordType.TXT, recordName, token)
                .CallAsync(_hc)
                .ConfigureAwait(false);
        }

        private async Task DeleteRecord(string recordName, string token, IAuthorizedSyntax context, Zone zone)
        {
            var dns = context.Zone(zone).Dns;
            var records = await dns
                .List()
                .OfType(DnsRecordType.TXT)
                .WithName(recordName)
                .WithContent(token)
                .Match(MatchType.All)
                .CallAsync(_hc)
                .ConfigureAwait(false);
            var record = records.FirstOrDefault();
            if (record != null)
            {
                await dns.Delete(record.Id)
                    .CallAsync(_hc)
                    .ConfigureAwait(false);
            }
        }

        public override async Task DeleteRecord(string recordName, string token)
        {
            var ctx = GetContext();
            var zone = await GetHostedZone(ctx, recordName).ConfigureAwait(false);
            await DeleteRecord(recordName, token, ctx, zone);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hc.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
