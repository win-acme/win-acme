using ACMESharp.Authorizations;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for DNS-01 validation plugins
    /// </summary>
    internal abstract class BaseDnsValidation<TOptions, TPlugin> : BaseValidation<TOptions, Dns01ChallengeValidationDetails>
    {
        public BaseDnsValidation(ILogService logService, TOptions options, string identifier) : 
            base(logService, options, identifier) { }

        public override void PrepareChallenge()
        {
            CreateRecord(_identifier, _challenge.DnsRecordName, _challenge.DnsRecordValue);
            _log.Information("Answer should now be available at {answerUri}", _challenge.DnsRecordName);
        }

        /// <summary>
        /// Delete record when we're done
        /// </summary>
        public override void CleanUp()
        {
            if (_challenge != null)
            {
                DeleteRecord(_identifier, _challenge.DnsRecordName);
            }
        }

        /// <summary>
        /// Delete validation record
        /// </summary>
        /// <param name="recordName">Name of the record</param>
        public abstract void DeleteRecord(string identifier, string recordName);

        /// <summary>
        /// Create validation record
        /// </summary>
        /// <param name="recordName">Name of the record</param>
        /// <param name="token">Contents of the record</param>
        public abstract void CreateRecord(string identifier, string recordName, string token);

    }
}
