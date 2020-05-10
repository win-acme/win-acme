using FluentCloudflare.Abstractions.Builders;
using FluentCloudflare.Api;
using FluentCloudflare.Api.Entities;
using FluentCloudflare.Extensions;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class Cloudflare : DnsValidation<Cloudflare>, IDisposable
    {
        private readonly CloudflareOptions _options;
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
        }

        private IAuthorizedSyntax GetContext()
        {
            // avoid name collision with this class
            return FluentCloudflare.Cloudflare.WithToken(_options.ApiToken.Value);
        }

        private async Task<Zone> GetHostedZone(IAuthorizedSyntax context, string recordName)
        {
            var zones = new List<Zone>();
            var response = await context.Zones.List().ParseAsync(_hc);
            while (response.Success && response.ResultInfo.Count > 0)
            {
                zones.AddRange(response.Unpack());
                response = await context.Zones.List().
                    Page(response.ResultInfo.Page + 1).
                    ParseAsync(_hc);
            }
            var zone = FindBestMatch(zones.ToDictionary(x => x.Name), recordName);
            if (zone == null)
            {
                _log.Error("Zone {domainName} could not be found using the Cloudflare API. Maybe you entered a wrong API Token or domain or the API Token does not allow access to this domain?", recordName);
                // maybe throwing would be better
                // this is how the Azure DNS Validator works
                return null;
            }
            return zone;
        }

        public override async Task CreateRecord(string recordName, string token)
        {
            var ctx = GetContext();
            var zone = await GetHostedZone(ctx, recordName).ConfigureAwait(false);
            if (zone == null)
            {
                throw new InvalidOperationException($"The zone could not be found using the Cloudflare API, thus creating a DNS validation record is impossible. " +
                    $"Please note you need to use an API Token, not the Global API Key. The token needs the permissions Zone.Zone:Read and Zone.DNS:Edit. Regarding " +
                    $"Zone:Read it is important, that this token has access to all zones in your account (Zone Resources > Include > All zones) because we need to " +
                    $"list your zones. Read the docs carefully for instructions.");
            }

            var dns = ctx.Zone(zone).Dns;
            _ = await dns.Create(DnsRecordType.TXT, recordName, token)
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
            if (record == null)
            {
                throw new Exception($"The record {recordName} that should be deleted does not exist at Cloudflare.");
            }

            _ = await dns.Delete(record.Id)
                .CallAsync(_hc)
                .ConfigureAwait(false);
        }

        public override async Task DeleteRecord(string recordName, string token)
        {
            var ctx = GetContext();
            var zone = await GetHostedZone(ctx, recordName).ConfigureAwait(false);
            await DeleteRecord(recordName, token, ctx, zone);
        }

        public void Dispose() => _hc.Dispose();
    }
}
