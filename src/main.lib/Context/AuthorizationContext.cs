using ACMESharp.Protocol.Resources;

namespace PKISharp.WACS.Context
{
    public class AuthorizationContext
    {
        public Authorization Authorization { get; }
        public OrderContext Order { get; }
        public string Uri { get; }
        public string Label { get; }
        public AuthorizationContext(OrderContext order, Authorization authorization, string uri)
        {
            Order = order;
            Authorization = authorization;
            Uri = uri;
            Label = authorization.Wildcard == true ? "*." : "" + authorization.Identifier.Value;
        }
    }
}
