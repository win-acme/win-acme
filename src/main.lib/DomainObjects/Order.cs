using PKISharp.WACS.Clients.Acme;
using System.Diagnostics;
using Protocol = ACMESharp.Protocol;

namespace PKISharp.WACS.DomainObjects
{
    [DebuggerDisplay("{CacheKeyPart}")]
    public class Order
    {
        public string? CacheKeyPart { get; set; }
        public string? FriendlyNamePart { get; set; }
        public string? KeyPath { get; set; }
        public Target Target { get; set; }
        public Renewal Renewal { get; set; }
        public Protocol.OrderDetails? Details { get; set; } = null;

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

        public bool? Valid => Details == null ? 
            null : 
            Details.Value.Payload.Status == AcmeClient.OrderValid || 
            Details.Value.Payload.Status == AcmeClient.OrderReady;

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
