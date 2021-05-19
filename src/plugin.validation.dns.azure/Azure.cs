using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Handle creation of DNS records in Azure
    /// </summary>
    internal class Azure : DnsValidation<Azure>
    {
        private DnsManagementClient _azureDnsClient;
        private readonly IProxyService _proxyService;
        private readonly AzureOptions _options;
        private readonly AzureHelpers _helpers;
        private readonly Dictionary<string, Dictionary<string, RecordSet>> _recordSets;
        private IEnumerable<Zone> _hostedZones;
        
        public Azure(AzureOptions options,
            LookupClientProvider dnsClient, 
            IProxyService proxyService,
            ILogService log, 
            ISettingsService settings) : base(dnsClient, log, settings)
        {
            _options = options;
            _proxyService = proxyService;
            _recordSets = new Dictionary<string, Dictionary<string, RecordSet>>();
            _helpers = new AzureHelpers(_options, log);
        }

        /// <summary>
        /// Allow this plugin to process multiple validations at the same time.
        /// They will still be prepared and cleaned in serial order though not
        /// to overwhelm the DnsManagementClient or risk threads overwriting 
        /// each others changes.
        /// </summary>
        public override ParallelOperations Parallelism => ParallelOperations.Answer;

        /// <summary>
        /// Create record in Azure DNS
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
            // Create or update record set parameters
            var txtRecord = new TxtRecord(new[] { record.Value });
            if (!_recordSets.ContainsKey(zone))
            {
                _recordSets.Add(zone, new Dictionary<string, RecordSet>());
            }
            var zoneRecords = _recordSets[zone];
            var relativeKey = RelativeRecordName(zone, record.Authority.Domain);
            if (!zoneRecords.ContainsKey(relativeKey))
            {
                zoneRecords.Add(
                    relativeKey, 
                    new RecordSet
                    {
                        TTL = 0,
                        TxtRecords = new List<TxtRecord> { txtRecord }
                    });
            } 
            else
            {
                zoneRecords[relativeKey].TxtRecords.Add(txtRecord);
            }
            return true;
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
                    updateTasks.Add(CreateOrUpdateRecordSet(zone, domain));
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
        private async Task CreateOrUpdateRecordSet(string zone, string domain)
        {
            try
            {
                var newSet = _recordSets[zone][domain];
                var client = await GetClient();
                try
                {
                    var originalSet = await client.RecordSets.GetAsync(_options.ResourceGroupName,
                                            zone,
                                            domain,
                                            RecordType.TXT);
                    _recordSets[zone][domain] = originalSet;
                } 
                catch
                {
                    _recordSets[zone][domain] = null;
                }
                if (newSet == null)
                {
                    await client.RecordSets.DeleteAsync(
                        _options.ResourceGroupName,
                        zone,
                        domain,
                        RecordType.TXT);
                } 
                else
                {
                    _ = await client.RecordSets.CreateOrUpdateAsync(
                        _options.ResourceGroupName,
                        zone,
                        domain,
                        RecordType.TXT,
                        newSet);
                }      
            } 
            catch (Exception ex)
            {
                _log.Error(ex, "Error updating DNS records in {zone} ({domain})", zone, domain);
            }
        }

        private async Task<DnsManagementClient> GetClient()
        {
            if (_azureDnsClient == null)
            {
                var credentials = await _helpers.GetCredentials();
                _azureDnsClient = new DnsManagementClient(credentials, _proxyService.GetHttpClient(), true)
                {
                    BaseUri = _helpers.ResourceManagersEndpoint,
                    SubscriptionId = _options.SubscriptionId
                };
            }
            return _azureDnsClient;
        }

        /// <summary>
        /// Find the appropriate hosting zone to use for record updates
        /// </summary>
        /// <param name="recordName"></param>
        /// <returns></returns>
        private async Task<string> GetHostedZone(string recordName)
        {
            if (!string.IsNullOrEmpty(_options.HostedZone))
            {
                return _options.HostedZone;
            }

            // Cache so we don't have to repeat this more than once for each renewal
            if (_hostedZones == null)
            {
                var client = await GetClient();
                var zones = new List<Zone>();
                var response = await client.Zones.ListByResourceGroupAsync(_options.ResourceGroupName);
                zones.AddRange(response);
                while (!string.IsNullOrEmpty(response.NextPageLink))
                {
                    response = await client.Zones.ListByResourceGroupNextAsync(response.NextPageLink);
                }
                _log.Debug("Found {count} hosted zones in Azure Resource Group {rg}", zones.Count, _options.ResourceGroupName);
                _hostedZones = zones;
            }

            var hostedZone = FindBestMatch(_hostedZones.ToDictionary(x => x.Name), recordName);
            if (hostedZone != null)
            {
                return hostedZone.Name;
            }
            _log.Error(
                "Can't find hosted zone for {recordName} in resource group {ResourceGroupName}",
                recordName,
                _options.ResourceGroupName);
            return null;
        }

        /// <summary>
        /// Ignored because we keep track of our list of changes
        /// </summary>
        /// <param name="record"></param>
        /// <returns></returns>
        public override Task DeleteRecord(DnsValidationRecord record) => Task.CompletedTask;

        /// <summary>
        /// Clear created
        /// </summary>
        /// <returns></returns>
        public override async Task Finalize() =>
            // We save the original record sets, so this should restore them
            await SaveChanges();
    }
}
