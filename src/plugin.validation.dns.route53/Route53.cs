using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Route53 : DnsValidation<Route53Options, Route53>
    {
        private readonly IAmazonRoute53 _route53Client;

        public Route53(LookupClientProvider dnsClient, ILogService log, Route53Options options, string identifier)
            : base(dnsClient, log, options, identifier)
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
                ResourceRecords = new List<ResourceRecord> { new ResourceRecord("\"" + value + "\"") },
                TTL = 1L
            };
        }

        public override void CreateRecord(string recordName, string token)
        {
            var hostedZoneId = GetHostedZoneId(recordName);

            if (hostedZoneId == null)
                return;

            _log.Information($"Creating TXT record {recordName} with value {token}");

            var response = _route53Client.ChangeResourceRecordSets(new ChangeResourceRecordSetsRequest(hostedZoneId,
                new ChangeBatch(new List<Change> { new Change(ChangeAction.UPSERT, CreateResourceRecordSet(recordName, token)) })));

            WaitChangesPropagation(response.ChangeInfo);
        }

        public override void DeleteRecord(string recordName, string token)
        {
            var hostedZoneId = GetHostedZoneId(recordName);

            if (hostedZoneId == null)
                return;

            _log.Information($"Deleting TXT record {recordName} with value {token}");

            _route53Client.ChangeResourceRecordSets(new ChangeResourceRecordSetsRequest(hostedZoneId,
                new ChangeBatch(new List<Change> { new Change(ChangeAction.DELETE, CreateResourceRecordSet(recordName, token)) })));
        }

        private string GetHostedZoneId(string recordName)
        {
            var domainName = _dnsClientProvider.DomainParser.GetRegisterableDomain(recordName);
            var response = _route53Client.ListHostedZones();
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

        private void WaitChangesPropagation(ChangeInfo changeInfo)
        {
            if (changeInfo.Status == ChangeStatus.INSYNC)
                return;

            _log.Information("Waiting for DNS changes propagation");

            var changeRequest = new GetChangeRequest(changeInfo.Id);

            while (_route53Client.GetChange(changeRequest).ChangeInfo.Status == ChangeStatus.PENDING)
                Thread.Sleep(TimeSpan.FromSeconds(5d));
        }
    }
}