using Microsoft.Azure.Management.Dns;
using Microsoft.Azure.Management.Dns.Models;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Rest;
using Microsoft.Rest.Azure.Authentication;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Handle creation of DNS records in Azure
    /// </summary>
    internal class Azure : DnsValidation<Azure>
    {
        private DnsManagementClient _azureDnsClient;
        private readonly Uri _resourceManagerEndpoint;
        private readonly ProxyService _proxyService;
        private readonly AzureOptions _options;
        private readonly Dictionary<string, Dictionary<string, RecordSet>> _recordSets;
        private IEnumerable<Zone> _hostedZones;
        
        public Azure(AzureOptions options,
            LookupClientProvider dnsClient, 
            ProxyService proxyService,
            ILogService log, 
            ISettingsService settings) : base(dnsClient, log, settings)
        {
            _options = options;
            _proxyService = proxyService;
            _recordSets = new Dictionary<string, Dictionary<string, RecordSet>>();
            _resourceManagerEndpoint = new Uri(AzureEnvironments.ResourceManagerUrls[AzureEnvironments.AzureCloud]);
            if (!string.IsNullOrEmpty(options.AzureEnvironment))
            {
                if (!AzureEnvironments.ResourceManagerUrls.TryGetValue(options.AzureEnvironment, out var endpoint))
                {
                    // Custom endpoint 
                    endpoint = options.AzureEnvironment;
                }
                try
                {
                    _resourceManagerEndpoint = new Uri(endpoint);
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Could not parse Azure endpoint url. Falling back to default.");
                }
            }
        }

        /// <summary>
        /// Allow this plugin to process multiple validations at the same time.
        /// They will still be prepared and cleaned in serial order though not
        /// to overwhelm the DnsManagementClient or risk threads overwriting 
        /// eachothers changes.
        /// </summary>
        public override ParallelOperations Parallelism => ParallelOperations.Answer;

        /// <summary>
        /// Create record in Azure DNS
        /// </summary>
        /// <param name="context"></param>
        /// <param name="recordName"></param>
        /// <param name="token"></param>
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
        /// <param name="recordSet"></param>
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
                // Build the service credentials and DNS management client
                ServiceClientCredentials credentials;

                // Decide between Managed Service Identity (MSI) 
                // and service principal with client credentials
                if (_options.UseMsi)
                {
                    var azureServiceTokenProvider = new AzureServiceTokenProvider();
                    var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync(_resourceManagerEndpoint.ToString());
                    credentials = new TokenCredentials(accessToken);
                }
                else
                {
                    credentials = await ApplicationTokenProvider.LoginSilentAsync(
                        _options.TenantId,
                        _options.ClientId,
                        _options.Secret.Value,
                        GetActiveDirectorySettingsForAzureEnvironment());
                }
                
                _azureDnsClient = new DnsManagementClient(credentials, _proxyService.GetHttpClient(), true)
                {
                    BaseUri = _resourceManagerEndpoint,
                    SubscriptionId = _options.SubscriptionId
                };
            }
            return _azureDnsClient;
        }

        private ActiveDirectoryServiceSettings GetActiveDirectorySettingsForAzureEnvironment()
        {
            return _options.AzureEnvironment switch
            {
                AzureEnvironments.AzureChinaCloud => ActiveDirectoryServiceSettings.AzureChina,
                AzureEnvironments.AzureUSGovernment => ActiveDirectoryServiceSettings.AzureUSGovernment,
                AzureEnvironments.AzureGermanCloud => ActiveDirectoryServiceSettings.AzureGermany,
                _ => ActiveDirectoryServiceSettings.Azure,
            };
        }

        /// <summary>
        /// Translate full host name to zone relative name
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
        private string RelativeRecordName(string zone, string recordName)
        {
            var ret = recordName.Substring(0, recordName.LastIndexOf(zone)).TrimEnd('.');
            return string.IsNullOrEmpty(ret) ? "@" : ret;
        }

        /// <summary>
        /// Find the approriate hosting zone to use for record updates
        /// </summary>
        /// <param name="recordName"></param>
        /// <returns></returns>
        private async Task<string> GetHostedZone(string recordName)
        {
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
        /// Clear created createds
        /// </summary>
        /// <returns></returns>
        public override async Task Finalize() =>
            // We save the original record sets, so this should restore them
            await SaveChanges();
    }
}
