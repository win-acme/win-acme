using PKISharp.WACS.DomainObjects;
using ACMESharp.Protocol.Resources;

namespace PKISharp.WACS.Context
{
    public class AuthorizationContext
    {
        public AcmeAuthorization Authorization { get; }
        public OrderContext Order { get; }
        public string Uri { get; }
        public string Label { get; }
        public AuthorizationContext(OrderContext order, AcmeAuthorization authorization, string uri)
        {
            Order = order;
            Authorization = authorization;
            Uri = uri;
            Label = Identifier.Parse(authorization).Unicode(true).Value;
        }
    }
}
