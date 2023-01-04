using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Single = PKISharp.WACS.Plugins.OrderPlugins.Single;

namespace PKISharp.WACS.Plugins.Resolvers
{
    internal class UnattendedResolver : IResolver
    {
        private readonly IPluginService _plugins;
        protected readonly MainArguments _arguments;
        protected readonly ISettingsService _settings;
        private readonly ILogService _log;
        private readonly PluginHelper _pluginHelper;

        public UnattendedResolver(
            ILogService log, 
            ISettingsService settings,
            MainArguments arguments,
            PluginHelper pluginHelper,
            IPluginService pluginService)
        {
            _log = log;
            _plugins = pluginService;
            _arguments = arguments;
            _settings = settings;
            _pluginHelper = pluginHelper;
        }

        private async Task<PluginFrontend<TOptionsFactory, TCapability>?> 
            GetPlugin<TOptionsFactory, TCapability>(
                Steps step,
                Type defaultBackend,
                string className,
                string? defaultParam1 = null,
                string? defaultParam2 = null,
                Func<IEnumerable<PluginFrontend<TOptionsFactory, TCapability>>, IEnumerable<PluginFrontend<TOptionsFactory, TCapability>>>? filter = null,
                Func<TCapability, (bool, string?)>? unusable = null)
                where TOptionsFactory : IPluginOptionsFactory
                where TCapability : IPluginCapability
        {
            // Helper method to determine final usability state
            // combination of plugin being enabled (e.g. due to missing
            // administrator rights) and being a right fit for the current
            // renewal (e.g. cannot validate wildcards using http-01)
            (bool, string?) disabledOrUnusable(PluginFrontend<TOptionsFactory, TCapability> plugin)
            {
                var disabled = plugin.Capability.Disabled;
                if (disabled.Item1)
                {
                    return disabled;
                }
                else if (unusable != null)
                {
                    return unusable(plugin.Capability);
                }
                return (false, null);
            };

            // Apply default sorting when no sorting has been provided yet
            IEnumerable<PluginFrontend<TOptionsFactory, TCapability>> options = new List<PluginFrontend<TOptionsFactory, TCapability>>();
            options = _plugins.
                GetPlugins(step).
                Select(_pluginHelper.Frontend<TOptionsFactory, TCapability>).
                ToList();
            options = filter != null ? filter(options) : options;
            var localOptions = options.Select(x => new {
                plugin = x,
                disabled = disabledOrUnusable(x)
            });

            // Default out when there are no reasonable options to pick
            if (!localOptions.Any() ||
                localOptions.All(x => x.disabled.Item1))
            {
                return null;
            }

            var changeInstructions = $"Choose another plugin using the --{className} switch or change the default in settings.json";
            if (!string.IsNullOrEmpty(defaultParam1))
            {
                var defaultPlugin = _plugins.GetPlugin(step, defaultParam1, defaultParam2);
                if (defaultPlugin == null)
                {
                    _log.Error("Unable to find {n} plugin {p}. " + changeInstructions, className, defaultParam1);
                    return null;
                }
                else
                {
                    defaultBackend = defaultPlugin.Backend;
                }
            }

            var defaultOption = localOptions.First(x => x.plugin.Meta.Backend == defaultBackend);
            var defaultTypeDisabled = defaultOption.disabled;
            if (defaultTypeDisabled.Item1)
            {
                _log.Error("{n} plugin {x} not available: {m}. " + changeInstructions, 
                    step, 
                    defaultOption.plugin.Meta.Name ?? "Unknown",
                    defaultTypeDisabled.Item2?.TrimEnd('.'));
                return null;
            }

            return defaultOption.plugin;
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// Renewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<PluginFrontend<PluginOptionsFactory<TargetPluginOptions>, IPluginCapability>?> GetTargetPlugin()
        {
            // NOTE: checking the default option here doesn't make 
            // sense because MainArguments.Source is what triggers
            // unattended mode in the first place. We woudn't even 
            // get into this code unless it was specified.
            return await GetPlugin<PluginOptionsFactory<TargetPluginOptions>, IPluginCapability>(
                Steps.Target,
                defaultParam1: string.IsNullOrWhiteSpace(_arguments.Source) ? _arguments.Target : _arguments.Source,
                defaultBackend: typeof(Manual),
                className: "source");
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this Renewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<PluginFrontend<PluginOptionsFactory<ValidationPluginOptions>, IValidationPluginCapability>?> GetValidationPlugin()
        {
            var defaultParam2 = _arguments.ValidationMode ?? _settings.Validation.DefaultValidationMode;
            if (string.IsNullOrEmpty(defaultParam2))
            {
                defaultParam2 = Constants.Http01ChallengeType;
            }
            return await GetPlugin<PluginOptionsFactory<ValidationPluginOptions>, IValidationPluginCapability>(
                Steps.Validation,
                defaultParam1: _arguments.Validation ?? _settings.Validation.DefaultValidation,
                defaultParam2: defaultParam2,
                defaultBackend: typeof(SelfHosting),
                unusable: (c) => (!c.CanValidate(), "Unsupported source. Most likely this is because you have included a wildcard identifier (*.example.com), which requires DNS validation."),
                className: "validation");
        }

        /// <summary>
        /// Get the OrderPlugin which is used to convert the target into orders 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<PluginFrontend<PluginOptionsFactory<OrderPluginOptions>, IOrderPluginCapability>?> GetOrderPlugin()
        {
            return await GetPlugin<PluginOptionsFactory<OrderPluginOptions>, IOrderPluginCapability>(
                Steps.Order,
                defaultParam1: _arguments.Order,
                defaultBackend: typeof(Single),
                unusable: (c) => (!c.CanProcess(), "Unsupported source. Most likely this is because you are using a custom CSR which doesn't allow the order to be split."),
                className: "order");
        }

        /// <summary>
        /// Get the CsrPlugin which is used to generate the private key 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<PluginFrontend<PluginOptionsFactory<CsrPluginOptions>, IPluginCapability>?> GetCsrPlugin()
        {
            return await GetPlugin<PluginOptionsFactory<CsrPluginOptions>, IPluginCapability>(
                Steps.Csr,
                defaultParam1: _arguments.Csr,
                defaultBackend: typeof(Rsa),
                className: "csr");
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<PluginFrontend<PluginOptionsFactory<StorePluginOptions>, IPluginCapability>?> GetStorePlugin(IEnumerable<Plugin> chosen)
        {
            var cmd = _arguments.Store ?? _settings.Store.DefaultStore;
            if (string.IsNullOrEmpty(cmd))
            {
                cmd = CertificateStoreOptions.PluginName;
            }
            var parts = cmd.ParseCsv();
            if (parts == null)
            {
                return null;
            }
            var index = chosen.Count();
            if (index == parts.Count)
            {
                return null;
            }
            return await GetPlugin<PluginOptionsFactory<StorePluginOptions>, IPluginCapability>(
                Steps.Store,
                filter: x => x,
                defaultParam1: parts[index],
                defaultBackend: typeof(StorePlugins.Null),
                className: "store");
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<PluginFrontend<PluginOptionsFactory<InstallationPluginOptions>, IInstallationPluginCapability>?> GetInstallationPlugin(IEnumerable<Plugin> chosenStores, IEnumerable<Plugin> chosenInstallation)
        {
            var cmd = _arguments.Installation ?? _settings.Installation.DefaultInstallation;
            var parts = cmd.ParseCsv();
            if (parts == null)
            {
                return null;
            }
            var index = chosenInstallation.Count();
            if (index == parts.Count)
            {
                return null;
            }
            return await GetPlugin<PluginOptionsFactory<InstallationPluginOptions>, IInstallationPluginCapability>(
                Steps.Installation,
                filter: x => x,
                unusable: x => { var (a, b) = x.CanInstall(chosenStores.Select(x => x.Backend), chosenInstallation.Select(x => x.Backend)); return (!a, b); },
                defaultParam1: parts[index],
                defaultBackend: typeof(InstallationPlugins.Null),
                className: "installation");
        }
    }
}
