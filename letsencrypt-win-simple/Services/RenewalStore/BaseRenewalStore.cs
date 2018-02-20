using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Services.RenewalStore
{
    abstract class BaseRenewalStore : IRenewalStoreService
    {
        internal List<ScheduledRenewal> _renewalsCache = null;
        internal SettingsService _settings = null;
        internal ILogService _log = null;

        public BaseRenewalStore(ILogService log, IOptionsService options, SettingsService settings)
        {
            _settings = settings;
            _log = log;
        }

        public IEnumerable<ScheduledRenewal> Renewals
        {
            get
            {
                if (_renewalsCache == null)
                {
                    if (RenewalStore != null)
                    {
                        _renewalsCache = RenewalStore.Select(x => Load(x, _settings.ConfigPath)).Where(x => x != null).ToList();
                    }
                    else
                    {
                        _renewalsCache = new List<ScheduledRenewal>();
                    }
                }
                return _renewalsCache;
            }
            set
            {
                _renewalsCache = value.ToList();
                _renewalsCache.ForEach(r =>
                {
                    if (r.Updated)
                    {
                        File.WriteAllText(HistoryFile(r, _settings.ConfigPath).FullName, JsonConvert.SerializeObject(r.History));
                        r.Updated = false;
                    }
                });
                RenewalStore = _renewalsCache.Select(x => JsonConvert.SerializeObject(x,
                    new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    })).ToArray();
            }
        }

        /// <summary>
        /// Actual storage
        /// </summary>
        abstract public string[] RenewalStore { get; set; }

        /// <summary>
        /// Parse from string
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private ScheduledRenewal Load(string renewal, string path)
        {
            ScheduledRenewal result;
            try
            {
                result = JsonConvert.DeserializeObject<ScheduledRenewal>(renewal);
                if (result == null || result.Binding == null)
                {
                    throw new Exception();
                }
            }
            catch
            {
                _log.Error("Unable to deserialize renewal: {renewal}", renewal);
                return null;
            }

            if (result.History == null)
            {
                result.History = new List<RenewResult>();
                var historyFile = HistoryFile(result, path);
                if (historyFile.Exists)
                {
                    try
                    {
                        result.History = JsonConvert.DeserializeObject<List<RenewResult>>(File.ReadAllText(historyFile.FullName));
                    }
                    catch
                    {
                        _log.Warning("Unable to read history file {path}", historyFile.Name);
                    }
                }
            }

            if (result.Binding.AlternativeNames == null)
            {
                result.Binding.AlternativeNames = new List<string>();
            }

            if (result.Binding.HostIsDns == null)
            {
                result.Binding.HostIsDns = !result.San;
            }

            if (result.Binding.IIS == null)
            {
                result.Binding.IIS = !(result.Binding.PluginName == nameof(Manual));
            }

            if (result.Binding.TargetSiteId == null)
            {
                result.Binding.TargetSiteId = result.Binding.SiteId;
            }

            return result;
        }

        /// <summary>
        /// Determine location and name of the history file
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="configPath"></param>
        /// <returns></returns>
        private FileInfo HistoryFile(ScheduledRenewal renewal, string configPath)
        {
            return new FileInfo(Path.Combine(configPath, $"{renewal.Binding.Host}.history.json"));
        }

    }
}
