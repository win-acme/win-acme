using ACMESharp.Authorizations;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using Serilog.Context;
using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for DNS-01 validation plugins
    /// </summary>
    public abstract class DnsValidation<TOptions, TPlugin> : Validation<TOptions, Dns01ChallengeValidationDetails>
    {
        protected LookupClientProvider _dnsClientProvider { get; private set; }

        protected DnsValidation(LookupClientProvider dnsClient, ILogService logService, TOptions options, string identifier) : 
            base(logService, options, identifier)
        {
            _dnsClientProvider = dnsClient;
        }

        public override void PrepareChallenge()
        {
            CreateRecord(_challenge.DnsRecordName, _challenge.DnsRecordValue);
            _log.Information("Answer should now be available at {answerUri}", _challenge.DnsRecordName);

            // Verify that the record was created succesfully and wait for possible
            // propagation/caching/TTL issues to resolve themselves naturally
            var retry = 0;
            var maxRetries = 5;
            var retrySeconds = 30;
            while (true)
            {
                if (PreValidate())
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

        protected bool PreValidate()
        {
            try
            {
                var domainName = _dnsClientProvider.DefaultClient.GetRootDomain(_challenge.DnsRecordName);
                LookupClientWrapper dnsClient;
                if (IPAddress.TryParse(Properties.Settings.Default.DnsServer, out IPAddress overrideNameServerIp))
                {
                    _log.Debug("Overriding the authoritative name server for {DomainName} with the configured name server {OverrideNameServerIp}", domainName, overrideNameServerIp);
                    dnsClient = _dnsClientProvider.GetClient(overrideNameServerIp);
                }
                else
                {
                    dnsClient = _dnsClientProvider.GetClient(domainName);
                }
                if (dnsClient.LookupClient.UseRandomNameServer)
                {
                    using (LogContext.PushProperty("NameServerIpAddresses", dnsClient.LookupClient.NameServers.Select(ns => ns.Endpoint.Address.ToString()), true))
                    {
                        _log.Debug("Using random name server");
                    }
                }
                var tokens = dnsClient.GetTextRecordValues(_challenge.DnsRecordName).ToList();
                if (tokens.Contains(_challenge.DnsRecordValue))
                {
                    _log.Information("Preliminary validation succeeded: {ExpectedTxtRecord} found in {TxtRecords}", _challenge.DnsRecordValue, String.Join(", ", tokens));
                    return true;
                }
                else if (!tokens.Any())
                {
                    _log.Warning("Preliminary validation failed: no TXT records found");
                }
                else
                {
                    _log.Warning("Preliminary validation failed: {ExpectedTxtRecord} not found in {TxtRecords}", _challenge.DnsRecordValue, String.Join(", ", tokens));
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Preliminary validation failed");
            }
            return false;
        }

        /// <summary>
        /// Delete record when we're done
        /// </summary>
        public override void CleanUp()
        {
            if (_challenge != null)
            {
                DeleteRecord(_challenge.DnsRecordName, _challenge.DnsRecordValue);
            }
        }

        /// <summary>
        /// Delete validation record
        /// </summary>
        /// <param name="recordName">Name of the record</param>
        public abstract void DeleteRecord(string recordName, string token);

        /// <summary>
        /// Create validation record
        /// </summary>
        /// <param name="recordName">Name of the record</param>
        /// <param name="token">Contents of the record</param>
        public abstract void CreateRecord(string recordName, string token);

    }
}
