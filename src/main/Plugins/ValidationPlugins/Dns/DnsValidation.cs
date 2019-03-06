using System;
using System.Linq;
using System.Net;
using ACMESharp.Authorizations;
using DnsClient;
using Nager.PublicSuffix;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Interfaces;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for DNS-01 validation plugins
    /// </summary>
    public abstract class DnsValidation<TOptions, TPlugin> : Validation<TOptions, Dns01ChallengeValidationDetails>
    {
        protected readonly DomainParser _domainParser;
        protected readonly ILookupClientProvider _lookupClientProvider;

        public DnsValidation(DomainParser domainParser, ILookupClientProvider lookupClientProvider, ILogService logService, TOptions options, string identifier) : 
            base(logService, options, identifier)
        {
            _domainParser = domainParser;
            _lookupClientProvider = lookupClientProvider;
        }

        public override void PrepareChallenge()
        {
            CreateRecord(_challenge.DnsRecordName, _challenge.DnsRecordValue);
            _log.Information("Answer should now be available at {answerUri}", _challenge.DnsRecordName);

            try
            {
                var domainName = _domainParser.Get(_challenge.DnsRecordName).RegistrableDomain;

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

                _log.Debug("Using DNS at IP {DomainNameServerIp}", lookupClient.NameServers.First().Endpoint.Address.ToString());

                var result = lookupClient.Query(_challenge.DnsRecordName, QueryType.TXT);
                var record = result.Answers.TxtRecords().FirstOrDefault();
                var value = record?.EscapedText?.FirstOrDefault();

                if (Equals(value, _challenge.DnsRecordValue))
                {
                    _log.Information("Preliminary validation looks good, but ACME will be more thorough...");
                }
                else
                {
                    _log.Warning("Preliminary validation failed, found {value} instead of {expected}", value ?? "(null)", _challenge.DnsRecordValue);
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
