using ACMESharp.Authorizations;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using Serilog.Context;
using System;
using System.Globalization;
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
                    var client = await _dnsClient.GetClients(Challenge.DnsRecordName);
                    var (_, cname) = await client.First().GetTextRecordValues(Challenge.DnsRecordName, 0);

                    // Normalize CNAME
                    var idn = new IdnMapping();
                    cname = cname.ToLower().Trim().TrimEnd('.');
                    cname = idn.GetAscii(cname);

                    // Substitute
                    if (cname != Challenge.DnsRecordName)
                    {
                        _log.Information("Detected that {DnsRecordName} is a CNAME that leads to {cname}", Challenge.DnsRecordName, cname);
                        _recordName = cname;
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
                var dnsClients = await _dnsClient.GetClients(Challenge.DnsRecordName, attempt);
                _log.Debug("Looking for TXT value {DnsRecordValue}...", Challenge.DnsRecordValue);
                foreach (var client in dnsClients)
                {
                    _log.Debug("Preliminary validation starting from {ip}...", client.IpAddress);
                    var (answers, server) = await client.GetTextRecordValues(Challenge.DnsRecordName, attempt);
                    _log.Debug("Preliminary validation retrieved answers from {server}", server);
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

    }
}
