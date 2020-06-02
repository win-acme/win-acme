using ACMESharp.Authorizations;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static PKISharp.WACS.Clients.DNS.LookupClientProvider;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for DNS-01 validation plugins
    /// </summary>reee
    public abstract class DnsValidation<TPlugin> : Validation<Dns01ChallengeValidationDetails>
    {
        protected readonly LookupClientProvider _dnsClient;
        protected readonly ILogService _log;
        protected readonly ISettingsService _settings;
        private DnsLookupResult? _authority;

        protected DnsValidation(
            LookupClientProvider dnsClient, 
            ILogService log,
            ISettingsService settings)
        {
            _dnsClient = dnsClient;
            _log = log;
            _settings = settings;
        }

        public override async Task PrepareChallenge()
        {
            // Check for substitute domains
            _authority = await _dnsClient.GetAuthority(
                Challenge.DnsRecordName, 
                followCnames: _settings.Validation.AllowDnsSubstitution);

            var success = false;
            while (!success) 
            {
                success = await CreateRecord(_authority.Domain, Challenge.DnsRecordValue);
                if (!success)
                {
                    if (_authority.From == null)
                    {
                        throw new Exception("Unable to prepare for challenge answer");
                    }
                    else
                    {
                        _authority = _authority.From;
                    }
                }
            } 

            // Verify that the record was created succesfully and wait for possible
            // propagation/caching/TTL issues to resolve themselves naturally
            var retry = 0;
            var maxRetries = _settings.Validation.PreValidateDnsRetryCount;
            var retrySeconds = _settings.Validation.PreValidateDnsRetryInterval;
            while (_settings.Validation.PreValidateDns)
            {
                if (await PreValidate())
                {
                    break;
                }
                else
                {
                    retry += 1;
                    if (retry > maxRetries)
                    {
                        _log.Information("It looks like validation is going to fail, but we will try now anyway...");
                        break;
                    }
                    else
                    {
                        _log.Information("Will retry in {s} seconds (retry {i}/{j})...", retrySeconds, retry, maxRetries);
                        Thread.Sleep(retrySeconds * 1000);
                    }
                }
            }
        }

        protected async Task<bool> PreValidate()
        {
            try
            {
                if (_authority == null)
                {
                    throw new InvalidOperationException("_recordName is null");
                }
                _log.Debug("Looking for TXT value {DnsRecordValue}...", _authority.Domain);
                foreach (var client in _authority.Nameservers)
                {
                    _log.Debug("Preliminary validation asking {ip}...", client.IpAddress);
                    var answers = await client.GetTxtRecords(_authority.Domain);
                    if (!answers.Any())
                    {
                        _log.Warning("Preliminary validation failed: no TXT records found");
                        return false;
                    }
                    if (!answers.Contains(Challenge.DnsRecordValue))
                    {
                        _log.Debug("Preliminary validation found values: {answers}", answers);
                        _log.Warning("Preliminary validation failed: incorrect TXT record(s) found");
                        return false;
                    }
                    _log.Debug("Preliminary validation from {ip} looks good", client.IpAddress);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Preliminary validation failed");
                return false;
            }
            _log.Information("Preliminary validation succeeded");
            return true;
        }

        /// <summary>
        /// Delete record when we're done
        /// </summary>
        public override async Task CleanUp()
        {
            if (HasChallenge && _authority != null)
            {
                try
                {
                    await DeleteRecord(_authority.Domain, Challenge.DnsRecordValue);
                } 
                catch (Exception ex)
                {
                    _log.Warning($"Error deleting record: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Delete validation record
        /// </summary>
        /// <param name="recordName">Name of the record</param>
        public abstract Task DeleteRecord(string recordName, string token);

        /// <summary>
        /// Create validation record
        /// </summary>
        /// <param name="recordName">Name of the record</param>
        /// <param name="token">Contents of the record</param>
        public abstract Task<bool> CreateRecord(string recordName, string token);

        /// <summary>
        /// Match DNS zone to use from a list of all zones
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="candidates"></param>
        /// <param name="recordName"></param>
        /// <returns></returns>
        public T? FindBestMatch<T>(Dictionary<string, T> candidates, string recordName) where T: class
        {
            var result = candidates.Keys.Select(key =>
            {
                var fit = 0;
                var name = key.TrimEnd('.');
                if (string.Equals(recordName, name, StringComparison.InvariantCultureIgnoreCase) || 
                    recordName.EndsWith("." + name, StringComparison.InvariantCultureIgnoreCase))
                {
                    // If there is a zone for a.b.c.com (4) and one for c.com (2)
                    // then the former is a better (more specific) match than the
                    // latter, so we should use that
                    fit = name.Split('.').Count();
                    _log.Verbose("Zone {name} scored {fit} points", key, fit);
                }
                else
                {
                    _log.Verbose("Zone {name} not matched", key);
                }
                return new { 
                    key, 
                    value = candidates[key],
                    fit
                };
            }).
            Where(x => x.fit > 0).
            OrderByDescending(x => x.fit).
            FirstOrDefault();

            if (result != null)
            {
                _log.Debug("Picked {name} as best match", result.key);
                return result.value;
            } 
            else
            {
                _log.Error("No match found");
                return null;
            }
        }
    }
}
