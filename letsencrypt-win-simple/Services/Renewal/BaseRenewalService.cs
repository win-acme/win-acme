using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.TargetPlugins;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Services.Renewal
{
    internal abstract class BaseRenewalService : IRenewalService
    {
        internal ILogService _log;
        internal string _baseUri;
        internal int _renewalDays;
        internal List<ScheduledRenewal> _renewalsCache;
        internal string _configPath = null;

        public BaseRenewalService(
            SettingsService settings,
            IOptionsService options,
            ILogService log)
        {
            _log = log;
            _configPath = settings.ConfigPath;
            _baseUri = options.Options.BaseUri;
            _renewalDays = settings.RenewalDays;
            _log.Debug("Renewal period: {RenewalDays} days", _renewalDays);
        }

        public ScheduledRenewal Find(Target target)
        {
            return Renewals.Where(r => string.Equals(r.Binding.Host, target.Host)).FirstOrDefault();
        }

        public void Save(ScheduledRenewal renewal, RenewResult result)
        {
            var renewals = Renewals.ToList();
            if (renewal.New)
            {
                renewal.History = new List<RenewResult>();
                renewals.Add(renewal);
                renewal.New = false;
                _log.Information(true, "Adding renewal for {target}", renewal.Binding.Host);

            }
            else if (result.Success)
            {
                _log.Information(true, "Renewal for {host} succeeded", renewal.Binding.Host);
            }
            else
            {
                _log.Error("Renewal for {host} failed, will retry on next run", renewal.Binding.Host);
            }

            // Set next date
            if (result.Success)
            {
                renewal.Date = DateTime.UtcNow.AddDays(_renewalDays);
                _log.Information(true, "Next renewal scheduled at {date}", renewal.Date.ToUserString());
            }
            renewal.Updated = true;
            renewal.History.Add(result);
            Renewals = renewals;
        }

        public IEnumerable<ScheduledRenewal> Renewals
        {
            get => ReadRenewals();
            set => WriteRenewals(value);
        }

        public void Cancel(ScheduledRenewal renewal)
        {
            Renewals = Renewals.Except(new[] { renewal });
            _log.Warning("Renewal {target} cancelled", renewal);
        }

        /// <summary>
        /// To be implemented by inherited classes (e.g. registry/filesystem/database)
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <returns></returns>
        internal abstract string[] ReadRenewalsRaw();

        /// <summary>
        /// To be implemented by inherited classes (e.g. registry/filesystem/database)
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <returns></returns>
        internal abstract void WriteRenewalsRaw(string[] Renewals);

        /// <summary>
        /// Parse renewals from store
        /// </summary>
        public IEnumerable<ScheduledRenewal> ReadRenewals()
        {
            if (_renewalsCache == null)
            {
                var read = ReadRenewalsRaw();
                var list = new List<ScheduledRenewal>();
                if (read != null)
                {
                    list.AddRange(read.Select(x => Load(x, _configPath)).Where(x => x != null));
                }
                _renewalsCache = list;
            }
            return _renewalsCache;
        }

        /// <summary>
        /// Serialize renewal information to store
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <param name="Renewals"></param>
        public void WriteRenewals(IEnumerable<ScheduledRenewal> Renewals)
        {
            var list = Renewals.ToList();
            list.ForEach(r =>
            {
                if (r.Updated)
                {
                    var history = HistoryFile(r, _configPath);
                    if (history != null) {
                        File.WriteAllText(history.FullName, JsonConvert.SerializeObject(r.History));
                    }
                    r.Updated = false;
                }
            });
            WriteRenewalsRaw(list.Select(x => JsonConvert.SerializeObject(x,
                new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                })).ToArray());
            _renewalsCache = list;
        }

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
                if (result?.Binding == null)
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
                if (historyFile != null && historyFile.Exists)
                {
                    try
                    {
                        var history = JsonConvert.DeserializeObject<List<RenewResult>>(File.ReadAllText(historyFile.FullName));
                        if (history != null)
                        {
                            result.History = history;
                        }
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

            if (string.IsNullOrWhiteSpace(result.Binding.SSLIPAddress))
            {
                result.Binding.SSLIPAddress = "*";
            }

            if (result.Binding.TargetSiteId == null && result.Binding.SiteId > 0)
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
            FileInfo fi = configPath.LongFile("", renewal.Binding.Host, ".history.json", _log);
            if (fi == null) {
                _log.Warning("Unable access history for {renewal]", renewal);
            }
            return fi;
        }

    }
}
