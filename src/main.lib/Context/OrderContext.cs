using Autofac;
using PKISharp.WACS.DomainObjects;

namespace PKISharp.WACS.Context
{
    /// <summary>
    /// Common objects used throughout the renewal process
    /// </summary>
    public class OrderContext
    {
        public ILifetimeScope ExecutionScope { get; private set; }
        public Order Order { get; private set; }
        public RunLevel RunLevel { get; private set; }
        public RenewResult Result { get; private set; }
        public Target Target => Order.Target;
        public Renewal Renewal => Order.Renewal;
        public CertificateInfo? PreviousCertificate { get; set; }
        public CertificateInfo? NewCertificate { get; set; }
        public string OrderName => Order.FriendlyNamePart ?? "Main";
        public bool ShouldRun { get; set; }
        public OrderContext(ILifetimeScope executionScope, Order order, RunLevel runLevel, bool shouldRun, RenewResult result)
        {
            ExecutionScope = executionScope;
            Order = order;
            RunLevel = runLevel;
            Result = result;
            ShouldRun = shouldRun;
        }
    }
}
