using Newtonsoft.Json;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class RenewalService : IRenewalService
    {
        internal ILogService _log;
        internal PluginService _plugin;
        internal int _renewalDays;
        internal List<ScheduledRenewal> _renewalsCache;
        internal string _configPath = null;

        public RenewalService(
            ISettingsService settings,
            IOptionsService options,
            ILogService log,
            PluginService plugin)
        {
            _log = log;
            _plugin = plugin;
            _configPath = settings.ConfigPath;
            _renewalDays = settings.RenewalDays;
            _log.Debug("Renewal period: {RenewalDays} days", _renewalDays);
        }

        public ScheduledRenewal Find(Target target)
        {
            return Renewals.Where(r => string.Equals(r.Target.Host, target.Host)).FirstOrDefault();
        }

        public void Save(ScheduledRenewal renewal, RenewResult result)
        {
            var renewals = Renewals.ToList();
            if (renewal.New)
            {
                renewal.History = new List<RenewResult>();
                renewals.Add(renewal);
                renewal.New = false;
                _log.Information(true, "Adding renewal for {target}", renewal.Target.Host);

            }
            else if (result.Success)
            {
                _log.Information(true, "Renewal for {host} succeeded", renewal.Target.Host);
            }
            else
            {
                _log.Error("Renewal for {host} failed, will retry on next run", renewal.Target.Host);
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

        public void Import(ScheduledRenewal renewal)
        {
            var renewals = Renewals.ToList();
            renewals.Add(renewal);
            _log.Information(true, "Importing renewal for {target}", renewal.Target.Host);
            Renewals = renewals;
        }

        public IEnumerable<ScheduledRenewal> Renewals
        {
            get => ReadRenewals();
            private set => WriteRenewals(value);
        }

        public void Cancel(ScheduledRenewal renewal)
        {
            Renewals = Renewals.Except(new[] { renewal });
            _log.Warning("Renewal {target} cancelled", renewal);
        }

        public void Clear()
        {
            Renewals = new List<ScheduledRenewal>();
        }
        
        /// <summary>
        /// Parse renewals from store
        /// </summary>
        public IEnumerable<ScheduledRenewal> ReadRenewals()
        {
            if (_renewalsCache == null)
            {
                var list = new List<ScheduledRenewal>();
                var di = new DirectoryInfo(_configPath);
                foreach (var rj in di.GetFiles("*.renewal.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        var result = JsonConvert.DeserializeObject<ScheduledRenewal>(
                            File.ReadAllText(rj.FullName),
                            new PluginOptionsConverter<StorePluginOptions>(_plugin.StorePluginOptionTypes()));
                        if (result?.Target == null)
                        {
                            throw new Exception();
                        }
                        if (result.Target.AlternativeNames == null)
                        {
                            result.Target.AlternativeNames = new List<string>();
                        }
                        list.Add(result);
                    }
                    catch
                    {
                        _log.Error("Unable to deserialize renewal: {renewal}", rj.Name);
                    }
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
            list.ForEach(renewal =>
            {
                if (renewal.Updated)
                {
                    var file = RenewalFile(renewal, _configPath);
                    if (file != null) {
                        File.WriteAllText(file.FullName, JsonConvert.SerializeObject(renewal, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            Formatting = Formatting.Indented
                        }));
                    }
                    renewal.Updated = false;
                }
            });
            _renewalsCache = list;
        }

        /// <summary>
        /// Determine location and name of the history file
        /// </summary>
        /// <param name="renewal"></param>
        /// <param name="configPath"></param>
        /// <returns></returns>
        private FileInfo RenewalFile(ScheduledRenewal renewal, string configPath)
        {
            FileInfo fi = configPath.LongFile("", renewal.Target.Host, ".renewal.json", _log);
            if (fi == null) {
                _log.Warning("Unable access file for {renewal]", renewal);
            }
            return fi;
        }
    }
}