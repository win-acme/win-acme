using ACMESharp.Authorizations;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private string? _recordName;

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
            if (_settings.Validation.AllowDnsSubstitution)
            {
                try
                {
                    // Resolve CNAME in DNS
                    var result = await _dnsClient.GetAuthority(Challenge.DnsRecordName);

                    // Substitute
                    if (result.Domain != Challenge.DnsRecordName)
                    {
                        _log.Information("Detected that {DnsRecordName} is a CNAME that leads to {cname}", Challenge.DnsRecordName, result.Domain);
                        _recordName = result.Domain;
                    }
                }
                catch (Exception ex)
                {
                    _log.Debug("Error checking for substitute domains: {ex}", ex.Message);
                }
            }

            // Create record
            await CreateRecord(_recordName ?? Challenge.DnsRecordName, Challenge.DnsRecordValue);
            _log.Information("Answer should now be available at {answerUri}", _recordName ?? Challenge.DnsRecordName);

            // Verify that the record was created succesfully and wait for possible
            // propagation/caching/TTL issues to resolve themselves naturally
            var retry = 0;
            var maxRetries = _settings.Validation.PreValidateDnsRetryCount;
            var retrySeconds = _settings.Validation.PreValidateDnsRetryInterval;
            while (_settings.Validation.PreValidateDns)
            {
                if (await PreValidate(retry))
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

        protected async Task<bool> PreValidate(int attempt)
        {
            try
            {
                var authority = await _dnsClient.GetAuthority(Challenge.DnsRecordName, attempt, true);
                // This should not be possible because authority was supposed to be 
                // checked recursively in the PrepareChallenge phase
                if (authority.Domain != Challenge.DnsRecordName)
                {
                    _log.Error("Unexpected authority");
                }
                _log.Debug("Looking for TXT value {DnsRecordValue}...", Challenge.DnsRecordValue);
                foreach (var client in authority.Nameservers)
                {
                    _log.Debug("Preliminary validation asking {ip}...", client.IpAddress);
                    var answers = await client.GetTxtRecords(Challenge.DnsRecordName);
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
            if (HasChallenge)
            {
                await DeleteRecord(_recordName ?? Challenge.DnsRecordName, Challenge.DnsRecordValue);;
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
        public abstract Task CreateRecord(string recordName, string token);

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
