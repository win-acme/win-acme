using Autofac;
using PKISharp.WACS.DomainObjects;
using System;
using System.Diagnostics;

namespace PKISharp.WACS.Context
{
    /// <summary>
    /// Common objects used throughout the renewal process
    /// </summary>
    [DebuggerDisplay("{OrderName}")]
    public class OrderContext
    {
        public const string DefaultOrderName = "Main";
        public ILifetimeScope OrderScope { get; private set; }
        public Order Order { get; private set; }
        public RunLevel RunLevel { get; private set; }
        public OrderResult OrderResult { get; private set; }
        public Target Target => Order.Target;
        public Renewal Renewal => Order.Renewal;
        public CertificateInfoCache? PreviousCertificate { get; set; }
        public ICertificateInfo? NewCertificate { get; set; }
        public string OrderName => Order.FriendlyNamePart ?? DefaultOrderName;
        public bool ShouldRun { get; set; }
        public OrderContext(ILifetimeScope orderScope, Order order, RunLevel runLevel)
        {
            OrderScope = orderScope;
            Order = order;
            RunLevel = runLevel;
            OrderResult = new OrderResult(OrderName);
        }
    }
}
