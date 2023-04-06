using ACMESharp.Protocol.Resources;
using Autofac;
using PKISharp.WACS.DomainObjects;
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
        public string OrderFriendlyName => Order.FriendlyNamePart ?? DefaultOrderName;
        public string OrderCacheKey => Order.CacheKeyPart ?? "main";
        public bool ShouldRun { get; set; }

        /// <summary>
        /// Previously issued certificate in the sequence, regardless of shape and validity
        /// </summary>
        public CertificateInfoCache? PreviousCertificate { get; set; }

        /// <summary>
        /// Matching cached certificate by shape, regardless of validity
        /// </summary>
        public CertificateInfoCache? CachedCertificate { get; set; }

        /// <summary>
        /// Server side renewal info for the CachedCertificate
        /// </summary>
        public AcmeRenewalInfo? RenewalInfo { get; set; }

        /// <summary>
        /// Actually usable certificate, either read from cache (while valid for reuse) 
        /// or from server, to be freshly issued
        /// </summary>
        public ICertificateInfo? NewCertificate { get; set; }

        public OrderContext(ILifetimeScope orderScope, Order order, RunLevel runLevel)
        {
            OrderScope = orderScope;
            Order = order;
            RunLevel = runLevel;
            OrderResult = new OrderResult(OrderCacheKey);
            ShouldRun = order.Renewal.New;
        }
    }
}
