using Nager.PublicSuffix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class DomainParseService
    {
        private const string Source = "https://publicsuffix.org/list/public_suffix_list.dat";
        private readonly DomainParser _parser;

        public DomainParseService(ILogService log, ProxyService proxy, ISettingsService settings)
        {
            var path = Path.Combine(Path.GetDirectoryName(settings.ExePath), "public_suffix_list.dat");
            _parser = new DomainParser(new FileTldRuleProvider(path));
            try
            {
                _parser = new DomainParser(new WebTldRuleProvider(proxy, settings));
            } 
            catch (Exception ex)
            {
                log.Warning("Error updating public suffix list from {source}: {ex}", Source, ex.Message);
            }
        }

        public string GetTLD(string fulldomain) => _parser.Get(fulldomain).TLD;
        public string GetDomain(string fulldomain) => _parser.Get(fulldomain).Domain;
        public string GetSubDomain(string fulldomain) => _parser.Get(fulldomain).SubDomain;
        
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

        /// <summary>
        /// Custom implementation so that we can use the proxy 
        /// that the user has configured and 
        /// </summary>
        private class WebTldRuleProvider : ITldRuleProvider
        {
            private readonly string _fileUrl;
            private readonly ProxyService _proxy;
            private readonly ICacheProvider _cache;

            public WebTldRuleProvider(ProxyService proxy, ISettingsService settings)
            {
                _fileUrl = "https://publicsuffix.org/list/public_suffix_list.dat";
                _cache = new FileCacheProvider(settings);
                _proxy = proxy;
            }

            public async Task<IEnumerable<TldRule>> BuildAsync()
            {
                var ruleParser = new TldRuleParser();
                string ruleData;
                if (!_cache.IsCacheValid())
                {
                    ruleData = await LoadFromUrl(_fileUrl).ConfigureAwait(false);
                    await _cache.SetAsync(ruleData).ConfigureAwait(false);
                }
                else
                {
                    ruleData = await _cache.GetAsync().ConfigureAwait(false);
                }
                var rules = ruleParser.ParseRules(ruleData);
                return rules;
            }

            public async Task<string> LoadFromUrl(string url)
            {
                using var httpClient = _proxy.GetHttpClient();
                using var response = await httpClient.GetAsync(url).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new RuleLoadException($"Cannot load from {url} {response.StatusCode}");
                }
                return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

    }
}
