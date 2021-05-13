using FluentCloudflare.Abstractions.Builders;
using FluentCloudflare.Api;
using FluentCloudflare.Api.Entities;
using FluentCloudflare.Extensions;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class Cloudflare : DnsValidation<Cloudflare>, IDisposable
    {
        private readonly CloudflareOptions _options;
        private readonly DomainParseService _domainParser;
        private readonly SecretServiceManager _ssm;
        private readonly HttpClient _hc;

        public Cloudflare(
            CloudflareOptions options,
            DomainParseService domainParser,
            IProxyService proxyService,
            LookupClientProvider dnsClient,
            SecretServiceManager ssm,
            ILogService log,
            ISettingsService settings) : base(dnsClient, log, settings)
        {
            _options = options;
            _hc = proxyService.GetHttpClient();
            _domainParser = domainParser;
            _ssm = ssm;
        }

        private IAuthorizedSyntax GetContext() =>
            // avoid name collision with this class
            FluentCloudflare.Cloudflare.WithToken(_ssm.EvaluateSecret(_options.ApiToken));

        private async Task<Zone> GetHostedZone(IAuthorizedSyntax context, string recordName)
        {
            var prs = _domainParser;
            var domainName = $"{prs.GetRegisterableDomain(recordName)}";
            var zonesResp = await context.Zones.List()
                .WithName(domainName)
                .ParseAsync(_hc)
                .ConfigureAwait(false);

            if (!zonesResp.Success || (zonesResp.Result?.Count ?? 0) < 1)
            {
                _log.Error("Zone {domainName} could not be found using the Cloudflare API." +
                    " Maybe you entered a wrong API Token or domain or the API Token does" +
                    " not allow access to this domain?", domainName);
                throw new Exception();
            }
            return zonesResp.Unpack().First();
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            var ctx = GetContext();
            var zone = await GetHostedZone(ctx, record.Authority.Domain).ConfigureAwait(false);
            if (zone == null)
            {
                _log.Error("The zone could not be found using the Cloudflare API, thus creating a DNS validation record is impossible. " +
                    $"Please note you need to use an API Token, not the Global API Key. The token needs the permissions Zone.Zone:Read and Zone.DNS:Edit. Regarding " +
                    $"Zone:Read it is important, that this token has access to all zones in your account (Zone Resources > Include > All zones) because we need to " +
                    $"list your zones. Read the docs carefully for instructions.");
                return false;
            }

            var dns = ctx.Zone(zone).Dns;
            _ = await dns.Create(DnsRecordType.TXT, record.Authority.Domain, record.Value)
                .CallAsync(_hc)
                .ConfigureAwait(false);
            return true;
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
                _log.Warning($"The record {recordName} that should be deleted does not exist at Cloudflare.");
                return;
            }

            try
            {
                _ = await dns.Delete(record.Id)
                    .CallAsync(_hc)
                    .ConfigureAwait(false);
            } 
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Cloudflare: {ex.Message}");
            }

        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            var ctx = GetContext();
            var zone = await GetHostedZone(ctx, record.Authority.Domain).ConfigureAwait(false);
            await DeleteRecord(record.Authority.Domain, record.Value, ctx, zone);
        }

        public void Dispose() => _hc.Dispose();
    }
}
