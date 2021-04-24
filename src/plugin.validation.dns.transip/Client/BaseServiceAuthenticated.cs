using PKISharp.WACS.Services;
using System.Net.Http;
using System.Threading.Tasks;

namespace TransIp.Library
{
    public abstract class BaseServiceAuthenticated : BaseService
    {
        private readonly AuthenticationService _authenticator;

        public BaseServiceAuthenticated(AuthenticationService authenticationService, IProxyService proxyService) : base(proxyService) => 
            _authenticator = authenticationService;

        protected internal override async Task<HttpClient> GetClient() => 
            await _authenticator.GetClient();
    }
}
