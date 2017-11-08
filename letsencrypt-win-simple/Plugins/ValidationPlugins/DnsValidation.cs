using ACMESharp;
using ACMESharp.ACME;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    abstract class DnsValidation : IValidationPlugin
    {
        protected ILogService _log;
        public virtual void Aquire(Target target) { }
        public virtual void Default(Target target) { }

        public DnsValidation(ILogService logService)
        {
            _log = logService;
        }

        public Action<AuthorizationState> PrepareChallenge(ScheduledRenewal renewal, AuthorizeChallenge challenge, string identifier)
        {
            var dnsChallenge = challenge.Challenge as DnsChallenge;
            var record = dnsChallenge.RecordName;
            CreateRecord(renewal.Binding, identifier, record, dnsChallenge.RecordValue);
            _log.Information("Answer should now be available at {answerUri}", record);
            return authzState => DeleteRecord(renewal.Binding, identifier, record);
        }

        /// <summary>
        /// Delete validation record
        /// </summary>
        /// <param name="recordName">where the answerFile should be located</param>
        public abstract void DeleteRecord(Target target, string identifier, string recordName);

        /// <summary>
        /// Create validation record
        /// </summary>
        /// <param name="recordName">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        public abstract void CreateRecord(Target target, string identifier, string recordName, string token);
    }
}
