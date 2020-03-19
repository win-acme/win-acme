using ACMESharp.Authorizations;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using Serilog.Context;
using System;
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
        protected readonly LookupClientProvider _dnsClientProvider;
        protected readonly ILogService _log;
        protected readonly ISettingsService _settings;

        protected DnsValidation(
            LookupClientProvider dnsClient, 
            ILogService log,
            ISettingsService settings)
        {
            _dnsClientProvider = dnsClient;
            _log = log;
            _settings = settings;
        }

        public override async Task PrepareChallenge()
        {
            await CreateRecord(Challenge.DnsRecordName, Challenge.DnsRecordValue);
            _log.Information("Answer should now be available at {answerUri}", Challenge.DnsRecordName);

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
                var dnsClients = await _dnsClientProvider.GetClients(Challenge.DnsRecordName, attempt);
                foreach (var client in dnsClients)
                {
                    _log.Debug("Preliminary validation will now check name server {ip}", client.IpAddress);
                    var answers = await client.GetTextRecordValues(Challenge.DnsRecordName, attempt);
                    if (!answers.Any())
                    {
                        _log.Warning("Preliminary validation at {address} failed: no TXT records found", client.IpAddress);
                        return false;
                    }
                    if (!answers.Contains(Challenge.DnsRecordValue))
                    {
                        _log.Warning("Preliminary validation at {address} failed: {ExpectedTxtRecord} not found in {TxtRecords}",
                            client.IpAddress,
                            Challenge.DnsRecordValue,
                            answers);
                        return false;
                    }
                    _log.Debug("Preliminary validation at {address} looks good!", client.IpAddress);
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
                await DeleteRecord(Challenge.DnsRecordName, Challenge.DnsRecordValue);
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
