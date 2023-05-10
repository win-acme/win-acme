using Azure;
using Azure.ResourceManager;
using Azure.ResourceManager.Dns;
using Azure.ResourceManager.Dns.Models;
using Azure.ResourceManager.Resources;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Policy;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]
[assembly: InternalsVisibleTo("wacs.test")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Handle creation of DNS records in Azure
    /// </summary>
    [IPlugin.Plugin<
        AzureOptions, AzureOptionsFactory,
        DnsValidationCapability, AzureJson>
        ("aa57b028-45fb-4aca-9cac-a63d94c76b4a",
        "Azure", "Create verification records in Azure DNS")]
    internal class Azure : DnsValidation<Azure>
    {
        private ArmClient? _armClient;
        private ResourceGroupResource? _resourceGroupResource;
        private SubscriptionResource? _subscriptionResource;

        private readonly AzureOptions _options;
        private readonly AzureHelpers _helpers;
        private readonly Dictionary<DnsZoneResource, Dictionary<string, DnsTxtRecordData>> _recordSets = new();
        private IEnumerable<DnsZoneResource>? _hostedZones;
        
        public Azure(AzureOptions options,
            LookupClientProvider dnsClient,
            SecretServiceManager ssm,
            IProxyService proxyService,
            ILogService log, 
            ISettingsService settings) : base(dnsClient, log, settings)
        {
            _options = options;
            _helpers = new AzureHelpers(options, proxyService, ssm);
        }

        /// <summary>
        /// Allow this plugin to process multiple validations at the same time.
        /// They will still be prepared and cleaned in serial order though not
        /// to overwhelm the DnsManagementClient or risk threads overwriting 
        /// each others changes.
        /// </summary>
        public override ParallelOperations Parallelism => ParallelOperations.Answer;

        /// <summary>
        /// Mark DNS record for creation
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            var zone = await GetHostedZone(record.Authority.Domain);
            if (zone == null)
            {
                return false;
            }
            var relativeKey = RelativeRecordName(zone.Data.Name, record.Authority.Domain);
            if (!_recordSets.ContainsKey(zone))
            {
                _recordSets.Add(zone, new());
            }
            if (!_recordSets[zone].ContainsKey(relativeKey))
            {
                try
                {
                    var existing = await zone.GetDnsTxtRecords().GetAsync(relativeKey);
                    _recordSets[zone].Add(relativeKey, existing.Value.Data);
                } 
                catch
                {
                    _recordSets[zone].Add(relativeKey, new DnsTxtRecordData() { TtlInSeconds = 60 });
                }
            }
            if (!_recordSets[zone][relativeKey].DnsTxtRecords.Any(x => x.Values.Contains(record.Value)))
            {
                var txtRecord = new DnsTxtRecordInfo();
                txtRecord.Values.Add(record.Value);
                _recordSets[zone][relativeKey].DnsTxtRecords.Add(txtRecord);
            }
            return true;
        }

        /// <summary>
        /// Mark DNS record for removal
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            var zone = await GetHostedZone(record.Authority.Domain);
            if (zone == null)
            {
                return;
            }
            var relativeKey = RelativeRecordName(zone.Data.Name, record.Authority.Domain);
            if (!_recordSets.ContainsKey(zone))
            {
                return;
            }
            if (!_recordSets[zone].ContainsKey(relativeKey))
            {
                return;
            }
            var txtResource = _recordSets[zone][relativeKey];
            var removeList = txtResource.DnsTxtRecords.Where(x => x.Values.Contains(record.Value)).ToList();
            foreach (var remove in removeList)
            {
                _ = txtResource.DnsTxtRecords.Remove(remove);
            }
        }

        /// <summary>
        /// Send all buffered changes to Azure
        /// </summary>
        /// <returns></returns>
        public override async Task SaveChanges()
        {
            var updateTasks = new List<Task>();
            foreach (var zone in _recordSets.Keys)
            {
                foreach (var domain in _recordSets[zone].Keys)
                {
                    updateTasks.Add(PersistRecordSet(zone, domain, _recordSets[zone][domain]));
                }
            }
            await Task.WhenAll(updateTasks);
        }

        /// <summary>
        /// Store a single recordset
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        private async Task PersistRecordSet(DnsZoneResource zone, string domain, DnsTxtRecordData txtRecords)
        {
            try
            {
                var txtRecordCollection = zone.GetDnsTxtRecords();
                if (!txtRecords.DnsTxtRecords.Any())
                {
                    var existing = await txtRecordCollection.GetAsync(domain);
                    if (existing != null)
                    {
                        _ = await existing.Value.DeleteAsync(WaitUntil.Started);
                    }
                }
                else
                {
                    _ = await txtRecordCollection.CreateOrUpdateAsync(WaitUntil.Completed, domain, txtRecords);
                }
            } 
            catch (Exception ex)
            {
                _log.Error(ex, "Error updating DNS records in {zone}", zone.Data.Name);
            }
        }

        private ArmClient Client
        {
            get
            {
                _armClient ??= new ArmClient(
                        _helpers.TokenCredential,
                        _options.SubscriptionId,
                        _helpers.ArmOptions);
                return _armClient;
            }
        }

        /// <summary>
        /// Get the subscription
        /// </summary>
        /// <returns></returns>
        private async Task<SubscriptionResource> Subscription()
        {
            if (_subscriptionResource != null)
            {
                _subscriptionResource = await Client.GetDefaultSubscriptionAsync();
            }
            if (_subscriptionResource == null)
            {
                throw new Exception($"Unable to find subscription {_options.SubscriptionId ?? "default"}");
            }
            return _subscriptionResource;
        }

        /// <summary>
        /// Find the appropriate hosting zone to use for record updates
        /// </summary>
        /// <param name="recordName"></param>
        /// <returns></returns>
        private async Task<DnsZoneResource?> GetHostedZone(string recordName)
        {
            var subscription = await Subscription();
          
            if (_hostedZones == null)
            {
                // Cache so we don't have to repeat this more than once for each renewal
                var cachedZones = new List<DnsZoneResource>();
                var zones = subscription.GetDnsZonesAsync();
                await foreach (var zone in zones)
                {
                    cachedZones.Add(zone);
                }
                _hostedZones = cachedZones;
            }

            // Option to bypass the best match finder
            if (!string.IsNullOrEmpty(_options.HostedZone))
            {
                var match = _hostedZones.FirstOrDefault(h => string.Equals(h.Data.Name, _options.HostedZone, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    _log.Error("Unable to find hosted zone {name}", _options.HostedZone);
                }
                return match;
            }

            var hostedZone = FindBestMatch(_hostedZones.ToDictionary(x => x.Data.Name), recordName);
            if (hostedZone != null)
            {
                return hostedZone;
            }
            _log.Error(
                "Can't find hosted zone for {recordName} in subscription {subscription}",
                recordName,
                _options.SubscriptionId);
            return null;
        }

        /// <summary>
        /// Clear created
        /// </summary>
        /// <returns></returns>
        public override async Task Finalize() =>
            // We save the original record sets, so this should restore them
            await SaveChanges();
    }
}
