using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for DNS-01 validation plugins
    /// </summary>
    abstract class BaseDnsValidation : BaseValidation<DnsChallenge>
    {
        public BaseDnsValidation(ILogService logService, string identifier) : 
            base(logService, identifier) { }

        public override void PrepareChallenge()
        {
            CreateRecord(_identifier, _challenge.RecordName, _challenge.RecordValue);
            _log.Information("Answer should now be available at {answerUri}", _challenge.RecordName);
        }

        /// <summary>
        /// Delete record when we're done
        /// </summary>
        public override void CleanUp()
        {
            DeleteRecord(_identifier, _challenge.RecordName);
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
