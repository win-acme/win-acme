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

            target.Plugin.CreateAuthorizationFile(dnsChallenge.RecordName, dnsChallenge.RecordValue);
            target.Plugin.BeforeAuthorize(target, dnsChallenge.RecordName, dnsChallenge.Token);
            var answerUri = dnsChallenge.RecordName;

            Program.Log.Information("Answer should now be available at {answerUri}", answerUri);

            return authzState =>
            {
                target.Plugin.DeleteAuthorization(dnsChallenge.RecordName, dnsChallenge.Token, null, null);
            };
        }
    }
}
