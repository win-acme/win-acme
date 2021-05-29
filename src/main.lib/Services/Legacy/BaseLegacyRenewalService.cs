using Newtonsoft.Json;
using PKISharp.WACS.Host.Services.Legacy;
using PKISharp.WACS.Plugins.TargetPlugins;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Services.Legacy
{
    internal abstract class BaseLegacyRenewalService : ILegacyRenewalService
    {
        internal ILogService _log;
        internal List<LegacyScheduledRenewal>? _renewalsCache;
        internal string? _configPath = null;

        public BaseLegacyRenewalService(
            LegacySettingsService settings,
            ILogService log)
        {
            _log = log;
            _configPath = settings.Client.ConfigurationPath;
        }

        public IEnumerable<LegacyScheduledRenewal> Renewals => ReadRenewals();

        /// <summary>
        /// To be implemented by inherited classes (e.g. registry/filesystem/database)
        /// </summary>
        /// <param name="BaseUri"></param>
        /// <returns></returns>
        internal abstract string[]? RenewalsRaw { get; }

        /// <summary>
        /// Parse renewals from store
        /// </summary>
        public IEnumerable<LegacyScheduledRenewal> ReadRenewals()
        {
            if (_renewalsCache == null)
            {
                var read = RenewalsRaw;
                var list = new List<LegacyScheduledRenewal>();
                if (read != null)
                {
                    list.AddRange(
                        read.Select(x => Load(x)).
                        Where(x => x != null).
                        OfType<LegacyScheduledRenewal>());
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
        private LegacyScheduledRenewal? Load(string renewal)
        {
            LegacyScheduledRenewal? result;
            try
            {
                result = JsonConvert.DeserializeObject<LegacyScheduledRenewal>(renewal);
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

            if (string.IsNullOrEmpty(result.Binding.TargetPluginName))
            {
                switch (result.Binding.PluginName)
                {
                    case "IIS":
                        result.Binding.TargetPluginName = result.Binding.HostIsDns == false ? "IISSite" : "IISBinding";
                        break;
                    case "IISSiteServer":
                        result.Binding.TargetPluginName = "IISSites";
                        break;
                    case "Manual":
                        result.Binding.TargetPluginName = "Manual";
                        break;
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
    }
}
