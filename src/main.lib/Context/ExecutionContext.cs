using Autofac;
using PKISharp.WACS.DomainObjects;

namespace PKISharp.WACS.Context
{
    /// <summary>
    /// Common objects used throughout the renewal process
    /// </summary>
    internal class ExecutionContext
    {
        public ILifetimeScope Scope { get; private set; }
        public Order Order { get; private set; }
        public RunLevel RunLevel { get; private set; }
        public RenewResult Result { get; private set; }
        public Target Target => Order.Target;
        public Renewal Renewal => Order.Renewal;
        public CertificateInfo? PreviousCertificate { get; set; }
        public CertificateInfo? NewCertificate { get; set; }
        public string OrderName => Order.FriendlyNamePart ?? "Main";
        public ExecutionContext(ILifetimeScope scope, Order order, RunLevel runLevel, RenewResult result)
        {
            Scope = scope;
            Order = order;
            RunLevel = runLevel;
            Result = result;
        }
    }
}
