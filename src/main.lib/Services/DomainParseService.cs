using Nager.PublicSuffix;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class DomainParseService
    {
        private readonly DomainParser _parser;

        public DomainParseService(ILogService log, ISettingsService settings)
        {
            try
            {
                _parser = new DomainParser(new WebTldRuleProvider(cacheProvider: new FileCacheProvider(settings)));
            } 
            catch (Exception ex)
            {
                log.Warning("Error retrieving public suffix list from https://publicsuffix.org/list/public_suffix_list.dat: {ex}", ex.Message);
            }
            if (_parser == null)
            {
                _parser = new DomainParser(new WebTldRuleProvider(cacheProvider: new StaticCacheProvider(settings)));
            }
        }

        public string GetTLD(string fulldomain) => _parser.Get(fulldomain).TLD;
        public string GetDomain(string fulldomain) => _parser.Get(fulldomain).Domain;
        public string GetSubDomain(string fulldomain) => _parser.Get(fulldomain).SubDomain;
        
        /// <summary>
        /// Load re-distributed file from program directory, which is always valid so that no outside
        /// request is needed. Also does not process any updates.
        /// </summary>
        private class StaticCacheProvider : ICacheProvider
        {
            private readonly string _value;

            public StaticCacheProvider(ISettingsService settings)
            {
                var path = Path.Combine(Path.GetDirectoryName(settings.ExePath), "public_suffix_list.dat");
                _value = File.ReadAllText(path);
            }

            public Task<string> GetAsync() => Task.FromResult(_value);

            public bool IsCacheValid() => true;

            public Task SetAsync(string val) => Task.CompletedTask;
        }

        /// <summary>
        /// Regular 7 day file cache in the configuration folder
        /// </summary>
        private class FileCacheProvider : ICacheProvider
        {
            private readonly FileInfo _file;
            
            public FileCacheProvider(ISettingsService settings)
            {
                var path = Path.Combine(settings.Client.ConfigurationPath, "public_suffix_list.dat");
                _file = new FileInfo(path);
            }

            public Task<string> GetAsync() => File.ReadAllTextAsync(_file.FullName);

            public bool IsCacheValid() => _file.Exists && _file.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-30);

            public Task SetAsync(string val) => File.WriteAllTextAsync(_file.FullName, val);
        }
    }
}
