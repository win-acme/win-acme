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

        private class MemoryCacheProvider : ICacheProvider
        {
            private string _value;

            public Task<string> GetValueAsync()
            {
                return Task.FromResult(_value);
            }

            public bool IsCacheValid()
            {
                return !string.IsNullOrEmpty(_value);
            }

            public Task SetValueAsync(string val)
            {
                _value = val;
                return Task.FromResult(true);
            }
        }
    }
}
