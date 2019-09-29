using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Route53 : DnsValidation<Route53>
    {
        private readonly IAmazonRoute53 _route53Client;

        public Route53(LookupClientProvider dnsClient, ILogService log, Route53Options options) : base(dnsClient, log)
        {
            var region = RegionEndpoint.USEast1;
            _route53Client = !string.IsNullOrWhiteSpace(options.IAMRole)
                ? new AmazonRoute53Client(new InstanceProfileAWSCredentials(options.IAMRole), region)
                : !string.IsNullOrWhiteSpace(options.AccessKeyId) && !string.IsNullOrWhiteSpace(options.SecretAccessKey.Value)
                    ? new AmazonRoute53Client(options.AccessKeyId, options.SecretAccessKey.Value, region)
                    : new AmazonRoute53Client(region);
        }

        private static ResourceRecordSet CreateResourceRecordSet(string name, string value)
        {
            return new ResourceRecordSet
            {
                Name = name,
                Type = RRType.TXT,
                ResourceRecords = new List<ResourceRecord> {
                    new ResourceRecord("\"" + value + "\"") },
                TTL = 1L
            };
        }

        public override async Task CreateRecord(string recordName, string token)
        {
            var hostedZoneId = await GetHostedZoneId(recordName);
            if (hostedZoneId != null)
            {
                _log.Information($"Creating TXT record {recordName} with value {token}");
                var response = await _route53Client.ChangeResourceRecordSetsAsync(
                    new ChangeResourceRecordSetsRequest(
                        hostedZoneId,
                        new ChangeBatch(new List<Change> {
                            new Change(
                                ChangeAction.UPSERT, 
                                CreateResourceRecordSet(recordName, token))
                        })));
                await WaitChangesPropagation(response.ChangeInfo);
            }
        }

        public override async Task DeleteRecord(string recordName, string token)
        {
            var hostedZoneId = await GetHostedZoneId(recordName);
            if (hostedZoneId != null)
            {
                _log.Information($"Deleting TXT record {recordName} with value {token}");
                await _route53Client.ChangeResourceRecordSetsAsync(
                    new ChangeResourceRecordSetsRequest(hostedZoneId,
                        new ChangeBatch(new List<Change> {
                            new Change(
                                ChangeAction.DELETE, 
                                CreateResourceRecordSet(recordName, token))
                        })));
            }
        }

        private async Task<string> GetHostedZoneId(string recordName)
        {
            var domainName = _dnsClientProvider.DomainParser.GetDomain(recordName);
            var response = await _route53Client.ListHostedZonesAsync();
            var hostedZone = response.HostedZones.Select(zone =>
            {
                var fit = 0;
                var name = zone.Name.TrimEnd('.').ToLowerInvariant();
                if (recordName.ToLowerInvariant().EndsWith(name))
                {
                    // If there is a zone for a.b.c.com (4) and one for c.com (2)
                    // then the former is a better (more specific) match than the 
                    // latter, so we should use that
                    fit = name.Split('.').Count();
                }
                return new { zone, fit };
            }).
            OrderByDescending(x => x.fit).
            FirstOrDefault();

            if (hostedZone != null)
            {
                return hostedZone.zone.Id;
            }

            _log.Error($"Can't find hosted zone for domain {domainName}");
            return null;
        }

        private async Task WaitChangesPropagation(ChangeInfo changeInfo)
        {
            if (changeInfo.Status == ChangeStatus.INSYNC)
            {
                return;
            }

            _log.Information("Waiting for DNS changes propagation");

            var changeRequest = new GetChangeRequest(changeInfo.Id);

            while ((await _route53Client.GetChangeAsync(changeRequest)).ChangeInfo.Status == ChangeStatus.PENDING)
            {
                await Task.Delay(5000);
            }
        }
    }
}