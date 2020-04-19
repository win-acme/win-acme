using acme = ACMESharp.Protocol;

namespace PKISharp.WACS.DomainObjects
{
    public class Order
    {
        public string? CacheKeyPart { get; set; }
        public string? FriendlyNamePart { get; set; }
        public Target Target { get; set; }
        public Renewal Renewal { get; set; }
        public acme.OrderDetails? Details { get; set; } = null;

        public Order(
            Renewal renewal,
            Target target,
            string? cacheKeyPart = null,
            string? friendlyNamePart = null)
        {
            Target = target;
            Renewal = renewal;
            CacheKeyPart = cacheKeyPart;
            FriendlyNamePart = friendlyNamePart;
        }
    }
}
