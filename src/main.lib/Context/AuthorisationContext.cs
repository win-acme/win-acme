using ACMESharp.Protocol.Resources;

namespace PKISharp.WACS.Context
{
    public class AuthorisationContext
    {
        public Authorization? Authorization { get; }
        public OrderContext Order { get; }

        public AuthorisationContext(OrderContext order, Authorization? authorization)
        {
            Order = order;
            Authorization = authorization;
        }
    }
}
