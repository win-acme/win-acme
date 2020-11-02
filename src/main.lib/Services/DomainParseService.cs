using Nager.PublicSuffix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class DomainParseService
    {
        private const string Source = "https://publicsuffix.org/list/public_suffix_list.dat";
        private DomainParser? _parser;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly ProxyService _proxy;
        private readonly VersionService _version;

        private DomainParser Parser
        {
            get
            {
                if (_parser == null)
                {
                    var path = Path.Combine(_version.ResourcePath, "public_suffix_list.dat");
                    try
                    {
                        _parser = new DomainParser(new FileTldRuleProvider(path));
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("Error loading static public suffix list from {path}: {ex}", path, ex.Message);
                    }
                    try
                    {
                        _parser = new DomainParser(new WebTldRuleProvider(_proxy, _log, _settings));
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("Error updating public suffix list from {source}: {ex}", Source, ex.Message);
                    }
                }
                if (_parser == null)
                {
                    throw new Exception("Public suffix list unavailable");
                }
                return _parser;
            }
        }

        public DomainParseService(ILogService log, ProxyService proxy, ISettingsService settings, VersionService version)
        {
            _log = log;
            _settings = settings;
            _proxy = proxy;
            _version = version;
        }

        public string GetTLD(string fulldomain) => Parser.Get(fulldomain).TLD;
        public string GetRegisterableDomain(string fulldomain) => Parser.Get(fulldomain).RegistrableDomain;

        /// <summary>
        /// Regular 7 day file cache in the configuration folder
        /// </summary>
        private class FileCacheProvider : ICacheProvider
        {
            private readonly FileInfo? _file;
            private string? _memoryCache;
            private readonly ILogService _log;

            public FileCacheProvider(ILogService log, ISettingsService settings)
            {
                _log = log;
                var path = Path.Combine(settings.Client.ConfigurationPath, "public_suffix_list.dat");
                _file = new FileInfo(path);
            }

            public async Task<string> GetAsync()
            {
                if (_file != null)
                {
                    try
                    {
                        _memoryCache = await File.ReadAllTextAsync(_file.FullName);
                    }
                    catch (Exception ex)
                    {
                        _log.Warning("Unable to read public suffix list cache from {path}: {ex}", _file.FullName, ex.Message);
                    };
                }
                return _memoryCache ?? "";
            }

            public bool IsCacheValid()
            {
                if (_file != null)
                {
                    return _file.Exists && _file.LastWriteTimeUtc > DateTime.UtcNow.AddDays(-30);
                }
                else
                {
                    return !string.IsNullOrEmpty(_memoryCache);
                }
            }

            public async Task SetAsync(string val) 
            {
                if (_file != null)
                {
                    try
                    {
                        await File.WriteAllTextAsync(_file.FullName, val);
                    } 
                    catch (Exception ex)
                    {
                        _log.Warning("Unable to write public suffix list cache to {path}: {ex}", _file.FullName, ex.Message);
                    }
                }
                _memoryCache = val;
            }
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

            public WebTldRuleProvider(ProxyService proxy, ILogService log, ISettingsService settings)
            {
                _fileUrl = "https://publicsuffix.org/list/public_suffix_list.dat";
                _cache = new FileCacheProvider(log, settings);
                _proxy = proxy;
            }

            public async Task<IEnumerable<TldRule>> BuildAsync()
            {
                var ruleParser = new TldRuleParser();
                string ruleData;
                if (!_cache.IsCacheValid())
                {
                    ruleData = await LoadFromUrl(_fileUrl);
                    await _cache.SetAsync(ruleData);
                }
                else
                {
                    ruleData = await _cache.GetAsync();
                }
                var rules = ruleParser.ParseRules(ruleData);
                return rules;
            }

            public async Task<string> LoadFromUrl(string url)
            {
                using var httpClient = _proxy.GetHttpClient();
                using var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    throw new RuleLoadException($"Cannot load from {url} {response.StatusCode}");
                }
                return await response.Content.ReadAsStringAsync();
            }
        }

    }
}
