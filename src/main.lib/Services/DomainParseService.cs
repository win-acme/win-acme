using Nager.PublicSuffix;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class DomainParseService
    {
        private readonly DomainParser _parser;

        public DomainParseService() => _parser = new DomainParser(new WebTldRuleProvider(cacheProvider: new MemoryCacheProvider()));
        public string GetTLD(string fulldomain) => _parser.Get(fulldomain).TLD;
        public string GetDomain(string fulldomain) => _parser.Get(fulldomain).Domain;
        public string GetSubDomain(string fulldomain) => _parser.Get(fulldomain).SubDomain;

        private class MemoryCacheProvider : ICacheProvider
        {
            private string _value;

            public Task<string> GetAsync() => Task.FromResult(_value);

            public bool IsCacheValid() => !string.IsNullOrEmpty(_value);

            public Task SetAsync(string val)
            {
                _value = val;
                return Task.FromResult(true);
            }
        }
    }
}
