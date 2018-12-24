using Newtonsoft.Json;
using PKISharp.WACS.DomainObjects.Legacy;
using PKISharp.WACS.Plugins.TargetPlugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Services.Legacy
{
    internal abstract class BaseLegacyRenewalService : ILegacyRenewalService
    {
        internal ILogService _log;
        internal List<ScheduledRenewal> _renewalsCache;
        internal string _configPath = null;

        public BaseLegacyRenewalService(
            ISettingsService settings,
            IOptionsService options,
            ILogService log)
        {
            _log = log;
            _configPath = settings.ConfigPath;
        }

        public IEnumerable<ScheduledRenewal> Renewals
        {
            get => ReadRenewals();
        }

        /// <summary>
        /// To be implemented by inherited classes (e.g. registry/filesystem/database)
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <returns></returns>
        internal abstract string[] RenewalsRaw { get; }

        /// <summary>
        /// Parse renewals from store
        /// </summary>
        public IEnumerable<ScheduledRenewal> ReadRenewals()
        {
            if (_renewalsCache == null)
            {
                var read = RenewalsRaw;
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
    }
}
