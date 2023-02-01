using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using System.Linq;

namespace PKISharp.WACS.Plugins.Base.Capabilities
{
    public abstract class ValidationCapability : DefaultCapability, IValidationPluginCapability
    {
        public abstract string ChallengeType { get; }
    }

    public class HttpValidationCapability : ValidationCapability
    {
        protected readonly Target Target;
        public HttpValidationCapability(Target target) => Target = target;
        public override string ChallengeType => Constants.Http01ChallengeType;
        public override State State => 
            Target.GetIdentifiers(false).Any(x => x.Value.StartsWith("*.")) ? 
            State.DisabledState("HTTP validation cannot be used for wildcard identifiers (e.g. *.example.com)") : 
            State.EnabledState();
    }

    public class DnsValidationCapability : ValidationCapability
    {
        protected readonly Target Target;
        public override string ChallengeType => Constants.Dns01ChallengeType;
        public DnsValidationCapability(Target target) => Target = target;
        public override State State =>
            !Target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName) ?
            State.DisabledState("DNS validation can only be used for DNS identifiers") :
            State.EnabledState();
    }

    public class TlsValidationCapability : ValidationCapability
    {
        protected readonly Target Target;
        public override string ChallengeType => Constants.TlsAlpn01ChallengeType;
        public TlsValidationCapability(Target target) => Target = target;
        public override State State =>
            Target.GetIdentifiers(false).Any(x => x.Value.StartsWith("*.")) ?
            State.DisabledState("TLS-ALPN validation cannot be used for wildcard identifiers (e.g. *.example.com)") :
            State.EnabledState();
    }
}
