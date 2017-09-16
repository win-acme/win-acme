using ACMESharp;
using ACMESharp.ACME;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins
{
    abstract class DnsValidation : IValidationPlugin
    {
        public string ChallengeType => AcmeProtocol.CHALLENGE_TYPE_DNS;

        public abstract string Name { get; }

        public Action<AuthorizationState> PrepareChallenge(Options options, Target target, AuthorizeChallenge challenge)
        {
            var dnsChallenge = challenge.Challenge as DnsChallenge;
            var record = dnsChallenge.RecordName;
            CreateRecord(record, dnsChallenge.RecordValue);
            Program.Log.Information("Answer should now be available at {answerUri}", record);
            return authzState => DeleteRecord(record);
        }

        /// <summary>
        /// Delete validation record
        /// </summary>
        /// <param name="recordName">where the answerFile should be located</param>
        public abstract void DeleteRecord(string recordName);

        /// <summary>
        /// Create validation record
        /// </summary>
        /// <param name="recordName">where the answerFile should be located</param>
        /// <param name="token">the token</param>
        public abstract void CreateRecord(string recordName, string token);

    }
}
