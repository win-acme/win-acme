using ACMESharp.Protocol.Resources;

namespace PKISharp.WACS.Context
{
    public class AuthorizationContext
    {
        public Authorization Authorization { get; }
        public OrderContext Order { get; }

        public AuthorizationContext(OrderContext order, Authorization authorization)
        {
            Order = order;
            Authorization = authorization;
        }
    }
}
