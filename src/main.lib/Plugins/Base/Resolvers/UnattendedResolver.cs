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
        private readonly MainArguments _arguments;
        private readonly ISettingsService _settings;
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

        private record PluginChoice<TOptionsFactory, TCapability>(
            PluginFrontend<TOptionsFactory, TCapability> Frontend,
            (bool, string?) Disabled)
            where TOptionsFactory : IPluginOptionsFactory
            where TCapability : IPluginCapability;

        private async Task<PluginFrontend<TOptionsFactory, TCapability>?> 
            GetPlugin<TOptionsFactory, TCapability>(
                Steps step,
                Type defaultBackend,
                string? defaultParam1 = null,
                string? defaultParam2 = null,
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
            var options = _plugins.
                GetPlugins(step).
                Select(_pluginHelper.Frontend<TOptionsFactory, TCapability>).
                Select(frontend => new PluginChoice<TOptionsFactory, TCapability>(frontend, disabledOrUnusable(frontend))).
                ToList();

            // Default out when there are no reasonable plugins to pick
            if (!options.Any() || options.All(x => x.Disabled.Item1))
            {
                return null;
            }

            var className = step.ToString().ToLower();
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

            var defaultOption = options.First(x => x.Frontend.Meta.Backend == defaultBackend);
            var defaultTypeDisabled = defaultOption.Disabled;
            if (defaultTypeDisabled.Item1)
            {
                _log.Error("{n} plugin {x} not available: {m}. " + changeInstructions, 
                    step, 
                    defaultOption.Frontend.Meta.Name ?? "Unknown",
                    defaultTypeDisabled.Item2?.TrimEnd('.'));
                return null;
            }

            return defaultOption.Frontend;
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// Renewal
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<TargetPluginOptions>, IPluginCapability>?> GetTargetPlugin()
        {
            // NOTE: checking the default option here doesn't make 
            // sense because MainArguments.Source is what triggers
            // unattended mode in the first place. We woudn't even 
            // get into this code unless it was specified.
            return await GetPlugin<PluginOptionsFactory<TargetPluginOptions>, IPluginCapability>(
                Steps.Source,
                defaultParam1: string.IsNullOrWhiteSpace(_arguments.Source) ? _arguments.Target : _arguments.Source,
                defaultBackend: typeof(Manual));
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this Renewal
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<ValidationPluginOptions>, IValidationPluginCapability>?> GetValidationPlugin()
        {
            var validationMode = _arguments.ValidationMode ?? _settings.Validation.DefaultValidationMode;
            if (string.IsNullOrEmpty(validationMode))
            {
                validationMode = Constants.Http01ChallengeType;
            }
            return await GetPlugin<PluginOptionsFactory<ValidationPluginOptions>, IValidationPluginCapability>(
                Steps.Validation,
                defaultParam1: _arguments.Validation ?? _settings.Validation.DefaultValidation,
                defaultParam2: validationMode,
                defaultBackend: typeof(SelfHosting),
                unusable: (c) => (!c.CanValidate(), "Unsupported source. Most likely this is because you have included a wildcard identifier (*.example.com), which requires DNS validation."));
        }

        /// <summary>
        /// Get the OrderPlugin which is used to convert the target into orders 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<OrderPluginOptions>, IOrderPluginCapability>?> GetOrderPlugin()
        {
            return await GetPlugin<PluginOptionsFactory<OrderPluginOptions>, IOrderPluginCapability>(
                Steps.Order,
                defaultParam1: _arguments.Order,
                defaultBackend: typeof(Single),
                unusable: (c) => (!c.CanProcess(), "Unsupported source. Most likely this is because you are using a custom CSR which doesn't allow the order to be split."));
        }

        /// <summary>
        /// Get the CsrPlugin which is used to generate the private key 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<CsrPluginOptions>, IPluginCapability>?> GetCsrPlugin()
        {
            return await GetPlugin<PluginOptionsFactory<CsrPluginOptions>, IPluginCapability>(
                Steps.Csr,
                defaultParam1: _arguments.Csr,
                defaultBackend: typeof(Rsa));
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<StorePluginOptions>, IPluginCapability>?> GetStorePlugin(IEnumerable<Plugin> chosen)
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
                defaultParam1: parts[index],
                defaultBackend: typeof(StorePlugins.Null));
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<InstallationPluginOptions>, IInstallationPluginCapability>?> 
            GetInstallationPlugin(IEnumerable<Plugin> stores, IEnumerable<Plugin> installation)
        {
            var cmd = _arguments.Installation ?? _settings.Installation.DefaultInstallation;
            var parts = cmd.ParseCsv();
            if (parts == null)
            {
                return null;
            }
            var index = installation.Count();
            if (index == parts.Count)
            {
                return null;
            }
            return await GetPlugin<PluginOptionsFactory<InstallationPluginOptions>, IInstallationPluginCapability>(
                Steps.Installation,
                unusable: x => { var (a, b) = x.CanInstall(stores.Select(x => x.Backend), installation.Select(x => x.Backend)); return (!a, b); },
                defaultParam1: parts[index],
                defaultBackend: typeof(InstallationPlugins.Null));
        }
    }
}
