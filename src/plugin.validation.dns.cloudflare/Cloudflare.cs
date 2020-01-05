using FluentCloudflare.Abstractions.Builders;
using FluentCloudflare.Api;
using FluentCloudflare.Api.Entities;
using FluentCloudflare.Extensions;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.ValidationPlugins;
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
        private readonly HttpClient _hc;

        public Cloudflare(
            CloudflareOptions options,
            LookupClientProvider dnsClient,
            ILogService log,
            ISettingsService settings)
            : base(dnsClient, log, settings)
        {
            _options = options;
            _hc = new HttpClient();
        }

        private IAuthorizedSyntax GetContext()
        {
            // avoid name collision with this class
            return FluentCloudflare.Cloudflare.WithToken(_options.ApiToken.Value);
        }

        private async Task<Zone> GetHostedZone(IAuthorizedSyntax context, string recordName)
        {
            var prs = _dnsClientProvider.DomainParser;
            var domainName = $"{prs.GetDomain(recordName)}.{prs.GetTLD(recordName)}";
            var zonesResp = await context.Zones.List().WithName(domainName).ParseAsync(_hc).ConfigureAwait(false);

            if (!zonesResp.Success || (zonesResp.Result?.Count ?? 0) < 1)
            {
                _log.Error(
                    "Can't find zone for {domainName} at cloudflare.",
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
            var dns = ctx.Zone(zone).Dns;
            await dns.Create(DnsRecordType.TXT, recordName, token).CallAsync(_hc).ConfigureAwait(false);
        }

        private async Task DeleteRecord(string recordName, string token, IAuthorizedSyntax context, Zone zone)
        {
            var dns = context.Zone(zone).Dns;
            var records = (await dns
                .List()
                .OfType(DnsRecordType.TXT)
                .WithName(recordName)
                .WithContent(token)
                .Match(MatchType.All)
                .CallAsync(_hc).ConfigureAwait(false));
            var record = records.FirstOrDefault();
            if (record != null)
            {
                await dns.Delete(record.Id).CallAsync(_hc).ConfigureAwait(false);
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
