using ACMESharp.Authorizations;
using DnsClient;
using PKISharp.WACS.Services;
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
                var lookup = new LookupClient(IPAddress.Parse(Properties.Settings.Default.DnsServer));
                var result = lookup.Query(_challenge.DnsRecordName, QueryType.TXT);
                var record = result.Answers.TxtRecords().FirstOrDefault();
                var value = record?.EscapedText?.FirstOrDefault();
                if (Equals(value, _challenge.DnsRecordValue))
                {
                    _log.Information("Preliminary validation looks good, but ACME can be more thorough...");
                }
            }
            catch
            {
                _log.Warning("Preliminary validation failed, found {value} instead of {expected}", 
                    foundValue ?? "(null)", 
                    _challenge.DnsRecordValue);
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
