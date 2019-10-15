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
            await CreateRecord(_challenge.DnsRecordName, _challenge.DnsRecordValue);
            _log.Information("Answer should now be available at {answerUri}", _challenge.DnsRecordName);

            // Verify that the record was created succesfully and wait for possible
            // propagation/caching/TTL issues to resolve themselves naturally
            var retry = 0;
            var maxRetries = 5;
            var retrySeconds = 30;
            while (_settings.Validation.PrevalidateDns)
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
                var dnsClients = await _dnsClientProvider.GetClients(_challenge.DnsRecordName, attempt);

                _log.Debug("Preliminary validation will now check name servers: {address}", 
                    string.Join(", ", dnsClients.Select(x => x.IpAddress)));
               
                // Parallel queries
                var answers = await Task.WhenAll(dnsClients.Select(client => client.GetTextRecordValues(_challenge.DnsRecordName, attempt)));

                // Loop through results
                for (var i = 0; i < dnsClients.Count(); i++)
                {
                    var currentClient = dnsClients[i];
                    var currentResult = answers[i];
                    if (!currentResult.Any())
                    {
                        _log.Warning("Preliminary validation for {address} failed: no TXT records found", currentClient.IpAddress);
                        return false;
                    }
                    if (!currentResult.Contains(_challenge.DnsRecordValue))
                    {
                        _log.Warning("Preliminary validation for {address} failed: {ExpectedTxtRecord} not found in {TxtRecords}", 
                            currentClient.IpAddress, 
                            _challenge.DnsRecordValue, 
                            string.Join(", ", currentResult));
                        return false;
                    }
                    _log.Debug("Preliminary validation for {address} looks good!", currentClient.IpAddress);
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
            if (_challenge != null)
            {
                await DeleteRecord(_challenge.DnsRecordName, _challenge.DnsRecordValue);
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
