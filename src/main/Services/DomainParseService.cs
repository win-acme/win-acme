using System.Threading.Tasks;
using Nager.PublicSuffix;

namespace PKISharp.WACS.Services
{
    public class DomainParseService
    {
        private DomainParser _parser;

        public DomainParseService()
        {
            _parser = new DomainParser(new WebTldRuleProvider(cacheProvider: new MemoryCacheProvider()));
        }

        public string GetRegisterableDomain(string fulldomain)
        {
            return _parser.Get(fulldomain).RegistrableDomain;
        }

        public string GetSubDomain(string fulldomain)
        {
            return _parser.Get(fulldomain).SubDomain;
        }

        private class MemoryCacheProvider : ICacheProvider
        {
            private string _value;

            public Task<string> GetAsync()
            {
                return Task.FromResult(_value);
            }

            public bool IsCacheValid()
            {
                return !string.IsNullOrEmpty(_value);
            }

            public Task SetAsync(string val)
            {
                _value = val;
                return Task.FromResult(true);
            }
        }
    }
}
