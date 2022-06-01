using Autofac;
using PKISharp.WACS.DomainObjects;
using System.Diagnostics;

namespace PKISharp.WACS.Context
{
    /// <summary>
    /// Common objects used throughout the renewal process
    /// </summary>
    [DebuggerDisplay("{Order.CacheKeyPart}")]
    public class OrderContext
    {
        public const string DefaultOrderName = "Main";
        public ILifetimeScope ExecutionScope { get; private set; }
        public Order Order { get; private set; }
        public RunLevel RunLevel { get; private set; }
        public RenewResult Result { get; private set; }
        public Target Target => Order.Target;
        public Renewal Renewal => Order.Renewal;
        public CertificateInfo? PreviousCertificate { get; set; }
        public CertificateInfo? NewCertificate { get; set; }
        public string OrderName => Order.FriendlyNamePart ?? DefaultOrderName;
        public bool ShouldRun { get; set; }
        public OrderContext(ILifetimeScope executionScope, Order order, RunLevel runLevel)
        {
            ExecutionScope = executionScope;
            Order = order;
            RunLevel = runLevel;
            Result = new RenewResult();
        }
    }
}
