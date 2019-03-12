using System;
using System.Linq;
using System.Net;
using ACMESharp.Authorizations;
using DnsClient;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Interfaces;
using Serilog.Context;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for DNS-01 validation plugins
    /// </summary>
    public abstract class DnsValidation<TOptions, TPlugin> : Validation<TOptions, Dns01ChallengeValidationDetails>
    {
        private readonly IDnsService _dnsService;
        private readonly ILookupClientProvider _lookupClientProvider;
        private readonly AcmeDnsValidationClient _acmeDnsValidationClient;

        protected DnsValidation(IDnsService dnsService, ILookupClientProvider lookupClientProvider, AcmeDnsValidationClient acmeDnsValidationClient, ILogService logService, TOptions options, string identifier) : 
            base(logService, options, identifier)
        {
            _dnsService = dnsService;
            _lookupClientProvider = lookupClientProvider;
            _acmeDnsValidationClient = acmeDnsValidationClient;
        }

        public override void PrepareChallenge()
        {
            CreateRecord(_challenge.DnsRecordName, _challenge.DnsRecordValue);
            _log.Information("Answer should now be available at {answerUri}", _challenge.DnsRecordName);

            try
            {
                var domainName = _dnsService.GetRootDomain(_challenge.DnsRecordName);

                ILookupClient lookupClient;

                if (IPAddress.TryParse(Properties.Settings.Default.DnsServer, out IPAddress overrideNameServerIp))
                {
                    _log.Debug("Overriding the authoritative name server for {DomainName} with the configured name server {OverrideNameServerIp}", domainName, overrideNameServerIp);
                    lookupClient = _lookupClientProvider.GetOrAdd(overrideNameServerIp);
                }
                else
                {
                    lookupClient = _lookupClientProvider.GetOrAdd(domainName);
                }

                if (lookupClient.UseRandomNameServer)
                {
                    using (LogContext.PushProperty("NameServerIpAddresses", lookupClient.NameServers.Select(ns => ns.Endpoint.Address.ToString()), true))
                    {
                        _log.Debug("Using random name server");
                    }
                }

                var tokens = _acmeDnsValidationClient.GetTextRecordValues(lookupClient, _challenge.DnsRecordName).ToList();

				if (tokens.Contains(_challenge.DnsRecordValue))
                {
                    _log.Information("Preliminary validation succeeded: {ExpectedTxtRecord} found in {TxtRecords}", _challenge.DnsRecordValue, String.Join(", ", tokens));
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
