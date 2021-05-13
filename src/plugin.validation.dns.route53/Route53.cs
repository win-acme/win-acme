using Amazon;
using Amazon.Route53;
using Amazon.Route53.Model;
using Amazon.Runtime;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Route53 : DnsValidation<Route53>
    {
        private readonly IAmazonRoute53 _route53Client;

        public Route53(
            LookupClientProvider dnsClient,
            ILogService log,
            IProxyService proxy,
            ISettingsService settings,
            SecretServiceManager ssm,
            Route53Options options) : base(dnsClient, log, settings)
        {
            var region = RegionEndpoint.USEast1;
            var config = new AmazonRoute53Config() { RegionEndpoint = region };
            config.SetWebProxy(proxy.GetWebProxy());
            _route53Client = !string.IsNullOrWhiteSpace(options.IAMRole)
                ? new AmazonRoute53Client(new InstanceProfileAWSCredentials(options.IAMRole), config)
                : !string.IsNullOrWhiteSpace(options.AccessKeyId)
                    ? new AmazonRoute53Client(options.AccessKeyId, ssm.EvaluateSecret(options.SecretAccessKey), config)
                    : new AmazonRoute53Client(config);
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

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var recordName = record.Authority.Domain;
                var token = record.Value;
                var hostedZoneIds = await GetHostedZoneIds(recordName);
                if (hostedZoneIds == null)
                {
                    return false;
                }
                _log.Information("Creating TXT record {recordName} with value {token}", recordName, token);
                var updateTasks = hostedZoneIds.Select(hostedZoneId =>
                    _route53Client.ChangeResourceRecordSetsAsync(
                                   new ChangeResourceRecordSetsRequest(
                                       hostedZoneId,
                                       new ChangeBatch(new List<Change> {
                                            new Change(
                                                ChangeAction.UPSERT,
                                                CreateResourceRecordSet(recordName, token))
                                        }))));
                var results = await Task.WhenAll(updateTasks);
                var propagationTasks = results.Select(result => WaitChangesPropagation(result.ChangeInfo));
                await Task.WhenAll(propagationTasks);
                return true;
            }
            catch (Exception ex)
            {
                _log.Warning($"Error creating TXT record: {ex.Message}");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            var recordName = record.Authority.Domain;
            var token = record.Value;
            var hostedZoneIds = await GetHostedZoneIds(recordName);
            _log.Information($"Deleting TXT record {recordName} with value {token}");
            var deleteTasks = hostedZoneIds.Select(hostedZoneId => 
                _route53Client.ChangeResourceRecordSetsAsync(
                    new ChangeResourceRecordSetsRequest(hostedZoneId,
                        new ChangeBatch(new List<Change> {
                    new Change(
                        ChangeAction.DELETE,
                        CreateResourceRecordSet(recordName, token))
                        }))));
            _ = await Task.WhenAll(deleteTasks);
        }

        private async Task<IEnumerable<string>> GetHostedZoneIds(string recordName)
        {
            var hostedZones = new List<HostedZone>();
            var response = await _route53Client.ListHostedZonesAsync();
            hostedZones.AddRange(response.HostedZones);
            while (response.IsTruncated)
            {
                response = await _route53Client.ListHostedZonesAsync(
                    new ListHostedZonesRequest() {
                        Marker = response.NextMarker
                    });
                hostedZones.AddRange(response.HostedZones);
            }
            _log.Debug("Found {count} hosted zones in AWS", hostedZones);

            hostedZones = hostedZones.Where(x => !x.Config.PrivateZone).ToList();
            var hostedZoneSets = hostedZones.GroupBy(x => x.Name);
            var hostedZone = FindBestMatch(hostedZoneSets.ToDictionary(x => x.Key), recordName);
            if (hostedZone != null)
            {
                return hostedZone.Select(x => x.Id);
            }
            _log.Error($"Can't find hosted zone for domain {recordName}");
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
                await Task.Delay(2000);
            }
        }
    }
}