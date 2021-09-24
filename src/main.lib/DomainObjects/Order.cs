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

        public string FriendlyNameBase
        {
            get
            {
                var friendlyNameBase = Renewal.FriendlyName;
                if (string.IsNullOrEmpty(friendlyNameBase))
                {
                    friendlyNameBase = Target.FriendlyName;
                }
                if (string.IsNullOrEmpty(friendlyNameBase))
                {
                    friendlyNameBase = Target.CommonName.Unicode(true).Value;
                }
                return friendlyNameBase;
            }
        }

        public string FriendlyNameIntermediate
        {
            get
            {
                var friendlyNameIntermediate = FriendlyNameBase;
                if (!string.IsNullOrEmpty(FriendlyNamePart))
                {
                    friendlyNameIntermediate += $" [{FriendlyNamePart}]";
                }
                return friendlyNameIntermediate;
            }
        }

    }
}
