using Nager.PublicSuffix;
using Nager.PublicSuffix.Exceptions;
using Nager.PublicSuffix.Extensions;
using Nager.PublicSuffix.Models;
using Nager.PublicSuffix.RuleParsers;
using Nager.PublicSuffix.RuleProviders;
using Nager.PublicSuffix.RuleProviders.CacheProviders;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class DomainParseService
    {
        private const string Source = "https://publicsuffix.org/list/public_suffix_list.dat";
        private readonly DomainParser _parser;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly IProxyService _proxy;

        private async Task<DomainParser> CreateParser()
        {
            var provider = default(IRuleProvider);
            var path = Path.Combine(VersionService.ResourcePath, "public_suffix_list.dat");
            try
            {
                var fileProvider = new LocalFileRuleProvider(path);
                if (await fileProvider.BuildAsync())
                {
                    provider = fileProvider;
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Error loading static public suffix list from {path}: {ex}", path, ex.Message);
            }
            try
            {
                var webProvider = new WebTldRuleProvider(_proxy, _log, _settings);
                if (await webProvider.BuildAsync())
                {
                    provider = webProvider;
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Error updating public suffix list from {source}: {ex}", Source, ex.Message);
            }
            if (provider == null)
            {
                throw new Exception("Public suffix list unavailable");
            } 
            return new DomainParser(provider);
        }

        public DomainParseService(ILogService log, IProxyService proxy, ISettingsService settings)
        {
            _log = log;
            _settings = settings;
            _proxy = proxy;
            _parser = CreateParser().Result;
        }

        public string GetTLD(string fulldomain) => _parser.Parse(fulldomain)?.TopLevelDomain ?? throw new Exception($"Unable to parse domain {fulldomain}");
        public string GetRegisterableDomain(string fulldomain) => _parser.Parse(fulldomain)?.RegistrableDomain ?? throw new Exception($"Unable to parse domain {fulldomain}");

        /// <summary>
        /// Regular 30 day file cache in the configuration folder
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

            public async Task<string?> GetAsync()
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
                return _memoryCache;
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
        private class WebTldRuleProvider : IRuleProvider
        {
            private readonly string _fileUrl;
            private readonly IProxyService _proxy;
            private readonly ICacheProvider _cache;
            private readonly DomainDataStructure _data;

            public WebTldRuleProvider(IProxyService proxy, ILogService log, ISettingsService settings)
            {
                _fileUrl = "https://publicsuffix.org/list/public_suffix_list.dat";
                _cache = new FileCacheProvider(log, settings);
                _proxy = proxy;
                _data = new DomainDataStructure("*", new TldRule("*"));
            }

            public async Task<bool> BuildAsync(bool ignoreCache = false, CancellationToken cancel = default)
            {
                string? ruleData;
                if (ignoreCache || !_cache.IsCacheValid())
                {
                    ruleData = await LoadFromUrl(_fileUrl);
                    await _cache.SetAsync(ruleData);
                }
                else
                {
                    ruleData = await _cache.GetAsync();
                }
                if (string.IsNullOrEmpty(ruleData))
                {
                    return false;
                }
                var ruleParser = new TldRuleParser();
                var enumerable = ruleParser.ParseRules(ruleData);
                _data.AddRules(enumerable);
                return true;
            }

            public DomainDataStructure? GetDomainDataStructure() => _data;

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
