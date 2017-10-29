using ACMESharp;
using ACMESharp.ACME;
using Autofac;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    abstract class DnsValidation : IValidationPlugin
    {
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_DNS;
        public abstract string Name { get; }
        public abstract string Description { get; }
        protected ILogService _log;

        public DnsValidation()
        {
            _log = Program.Container.Resolve<ILogService>();
        }

        public Action<AuthorizationState> PrepareChallenge(Target target, AuthorizeChallenge challenge, string identifier, Options options, InputService input)
        {
            var dnsChallenge = challenge.Challenge as DnsChallenge;
            var record = dnsChallenge.RecordName;
            CreateRecord(target, identifier, record, dnsChallenge.RecordValue);
            _log.Information("Answer should now be available at {answerUri}", record);
            return authzState => DeleteRecord(target, identifier, record);
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

        /// <summary>
        /// Should this validation option be shown for the target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public bool CanValidate(Target target)
        {
            return true;
        }

        public abstract void Aquire(IOptionsService options, InputService input, Target target);
        public abstract void Default(IOptionsService options, Target target);

        /// <summary>
        /// Create instance for specific target
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public virtual IValidationPlugin CreateInstance(Target target)
        {
            return this;
        }

    }
}
