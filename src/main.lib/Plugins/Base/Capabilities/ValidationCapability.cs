using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using System.Linq;

namespace PKISharp.WACS.Plugins.Base.Capabilities
{
    public abstract class ValidationCapability : DefaultCapability, IValidationPluginCapability
    {
        public abstract bool CanValidate();
    }

    public class HttpValidationCapability : ValidationCapability
    {
        protected readonly Target Target;
        public HttpValidationCapability(Target target) => Target = target;
        public override bool CanValidate() => !Target.GetIdentifiers(false).Any(x => x.Value.StartsWith("*."));
    }

    public class DnsValidationCapability : ValidationCapability
    {
        protected readonly Target Target;
        public DnsValidationCapability(Target target) => Target = target;
        public override bool CanValidate() => Target.Parts.SelectMany(x => x.Identifiers).All(x => x.Type == IdentifierType.DnsName);
    }

    public class TlsValidationCapability : ValidationCapability
    {
        protected readonly Target Target;
        public TlsValidationCapability(Target target) => Target = target;
        public override bool CanValidate() => !Target.GetIdentifiers(false).Any(x => x.Value.StartsWith("*."));
    }
}
