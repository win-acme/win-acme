using ACMESharp.Authorizations;
using DnsClient;
using PKISharp.WACS.Services;
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
        public DnsValidation(ILogService logService, TOptions options, string identifier) : 
            base(logService, options, identifier) { }

        public override void PrepareChallenge()
        {
            CreateRecord(_challenge.DnsRecordName, _challenge.DnsRecordValue);
            _log.Information("Answer should now be available at {answerUri}", _challenge.DnsRecordName);

            string foundValue = null;
            try
            {
                if (IPAddress.TryParse(Properties.Settings.Default.DnsServer, out IPAddress address)) 
                {
                    var lookup = new LookupClient(address);
                    var result = lookup.Query(_challenge.DnsRecordName, QueryType.TXT);
                    var record = result.Answers.TxtRecords().FirstOrDefault();
                    var value = record?.EscapedText?.FirstOrDefault();
                    if (Equals(value, _challenge.DnsRecordValue))
                    {
                        _log.Information("Preliminary validation looks good, but ACME will be more thorough...");
                    }
                    else
                    {
                        _log.Warning("Preliminary validation failed, found {value} instead of {expected}", foundValue ?? "(null)", _challenge.DnsRecordValue);
                    }
                }
                else
                {
                    _log.Warning("Invalid DNS server IP {server}, skip preliminary validation", Properties.Settings.Default.DnsServer);
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
