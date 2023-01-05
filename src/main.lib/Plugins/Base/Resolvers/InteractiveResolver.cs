using Autofac;
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
    internal class InteractiveResolver : IResolver
    {
        private readonly IPluginService _plugins;
        private readonly MainArguments _arguments;
        private readonly ISettingsService _settings;
        private readonly ILogService _log;
        private readonly PluginHelper _pluginHelper;
        private readonly IInputService _input;
        private readonly RunLevel _runLevel;

        public InteractiveResolver(
            ILogService log,
            IInputService inputService,
            ISettingsService settings,
            MainArguments arguments,
            IPluginService pluginService,
            PluginHelper pluginHelper,
            RunLevel runLevel)
        {
            _log = log;
            _settings = settings;
            _arguments = arguments;
            _input = inputService;
            _runLevel = runLevel;
            _pluginHelper = pluginHelper;
            _plugins = pluginService;
        }

        private record struct PluginChoice<TOptionsFactory, TCapability>(
            PluginFrontend<TOptionsFactory, TCapability> Frontend,
            (bool, string?) Disabled,
            string Description,
            bool Default)
            where TOptionsFactory : IPluginOptionsFactory
            where TCapability : IPluginCapability;

        private async Task<PluginFrontend<TOptionsFactory, TCapability>?> 
            GetPlugin<TOptionsFactory, TCapability>(
                Steps step,
                IEnumerable<Type> defaultBackends,
                string shortDescription,
                string longDescription,
                string? defaultParam1 = null,
                string? defaultParam2 = null,
                Func<PluginFrontend<TOptionsFactory, TCapability>, int>? sort = null, 
                Func<TCapability, (bool, string?)>? unusable = null,
                Func<Plugin, string>? description = null,
                bool allowAbort = true) 
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
                OrderBy(sort ??= (x) => 1).
                ThenBy(x => x.OptionsFactory.Order).
                ThenBy(x => x.Meta.Description).
                Where(x => !x.Meta.Hidden).
                Select(plugin => new PluginChoice<TOptionsFactory, TCapability>(
                    plugin, disabledOrUnusable(plugin),
                    description == null ? plugin.Meta.Description : description(plugin.Meta),
                    false)).
                ToList();
           
            // Default out when there are no reasonable plugins to pick
            if (!options.Any() || options.All(x => x.Disabled.Item1))
            {
                return null;
            }

            // Always show the menu in advanced mode, only when no default
            // selection can be made in simple mode
            var className = step.ToString().ToLower();
            var showMenu = _runLevel.HasFlag(RunLevel.Advanced);
            if (!string.IsNullOrEmpty(defaultParam1))
            {
                var defaultPlugin = _plugins.GetPlugin(step, defaultParam1, defaultParam2);
                if (defaultPlugin != null)
                {
                    defaultBackends = defaultBackends.Prepend(defaultPlugin.Backend);
                } 
                else
                {
                    _log.Error("Unable to find {n} plugin {p}", className, defaultParam1);
                    showMenu = true;
                }
            }

            var defaultOption = default(PluginChoice<TOptionsFactory, TCapability>);
            foreach (var backend in defaultBackends.Distinct())
            {
                defaultOption = options.FirstOrDefault(x => x.Frontend.Meta.Backend == backend);
                var defaultTypeDisabled = defaultOption.Disabled;
                if (defaultTypeDisabled.Item1)
                {
                    _log.Warning("{n} plugin {x} not available: {m}",
                        char.ToUpper(className[0]) + className[1..],
                        defaultOption.Frontend.Meta.Name ?? backend.Name,
                        defaultTypeDisabled.Item2);
                    showMenu = true;
                }
                else
                {
                    defaultOption.Default = true;
                    break;
                }
            }

            if (!showMenu)
            {
                return defaultOption.Frontend;
            }

            // List plugins for generating new certificates
            if (!string.IsNullOrEmpty(longDescription))
            {
                _input.CreateSpace();
                _input.Show(null, longDescription);
            }

            Choice<PluginFrontend<TOptionsFactory, TCapability>?> creator(PluginChoice<TOptionsFactory, TCapability> choice) {
                return Choice.Create<PluginFrontend<TOptionsFactory, TCapability>?>(
                       choice.Frontend,
                       description: choice.Description,
                       @default: choice.Default,
                       disabled: choice.Disabled);
            }

            return allowAbort
                ? await _input.ChooseOptional(shortDescription, options, creator, "Abort")
                : await _input.ChooseRequired(shortDescription, options, creator);
        }

        /// <summary>
        /// Allow user to choose a TargetPlugin
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<TargetPluginOptions>, IPluginCapability>?> GetTargetPlugin()
        {
            return await GetPlugin<PluginOptionsFactory<TargetPluginOptions>, IPluginCapability>(
                Steps.Source,
                defaultParam1: _settings.Source.DefaultSource,
                defaultBackends: new List<Type>() { typeof(IIS), typeof(Manual) },
                shortDescription: "How shall we determine the domain(s) to include in the certificate?",
                longDescription: "Please specify how the list of domain names that will be included in the certificate " +
                    "should be determined. If you choose for one of the \"all bindings\" options, the list will automatically be " +
                    "updated for future renewals to reflect the bindings at that time.");;
        }

        /// <summary>
        /// Allow user to choose a ValidationPlugin
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<ValidationPluginOptions>, IValidationPluginCapability>?> GetValidationPlugin()
        {
            var defaultParam1 = _settings.Validation.DefaultValidation;
            var defaultParam2 = _settings.Validation.DefaultValidationMode;
            if (string.IsNullOrEmpty(defaultParam2))
            {
                defaultParam2 = Constants.Http01ChallengeType;
            }
            if (!string.IsNullOrWhiteSpace(_arguments.Validation))
            {
                defaultParam1 = _arguments.Validation;
            }
            if (!string.IsNullOrWhiteSpace(_arguments.ValidationMode))
            {
                defaultParam2 = _arguments.ValidationMode;
            }
            return await GetPlugin<PluginOptionsFactory<ValidationPluginOptions>, IValidationPluginCapability>(
                Steps.Validation,
                sort: frontend =>
                {
                    return frontend.Meta.ChallengeType switch
                    {
                        Constants.Http01ChallengeType => 0,
                        Constants.Dns01ChallengeType => 1,
                        Constants.TlsAlpn01ChallengeType => 2,
                        _ => 3,
                    };
                },
                unusable: x => (!x.CanValidate(), "Unsuppored target. Most likely this is because you have included a wildcard identifier (*.example.com), which requires DNS validation."),
                description: x => $"[{x.ChallengeType}] {x.Description}",
                defaultParam1: defaultParam1,
                defaultParam2: defaultParam2,
                defaultBackends: new List<Type>() { typeof(SelfHosting), typeof(FileSystem) },
                shortDescription: "How would you like prove ownership for the domain(s)?",
                longDescription: "The ACME server will need to verify that you are the owner of the domain names that you are requesting" +
                    " the certificate for. This happens both during initial setup *and* for every future renewal. There are two main methods of doing so: " +
                    "answering specific http requests (http-01) or create specific dns records (dns-01). For wildcard domains the latter is the only option. " +
                    "Various additional plugins are available from https://github.com/win-acme/win-acme/.");
        }

        public async Task<PluginFrontend<PluginOptionsFactory<OrderPluginOptions>, IOrderPluginCapability>?> GetOrderPlugin()
        {
            return await GetPlugin<PluginOptionsFactory<OrderPluginOptions>, IOrderPluginCapability>(
                   Steps.Order,
                   defaultParam1: _settings.Order.DefaultPlugin,
                   defaultBackends: new List<Type>() { typeof(Single) },
                   unusable: (c) => (!c.CanProcess(), "Unsupported source."),
                   shortDescription: "Would you like to split this source into multiple certificates?",
                   longDescription: $"By default your source hosts are covered by a single certificate. " +
                        $"But if you want to avoid the {Constants.MaxNames} domain limit, want to prevent " +
                        $"information disclosure via the SAN list, and/or reduce the impact of a single validation failure," +
                        $"you may choose to convert one source into multiple certificates, using different strategies.");
        }

        public async Task<PluginFrontend<PluginOptionsFactory<CsrPluginOptions>, IPluginCapability>?> GetCsrPlugin()
        {
            return await GetPlugin<PluginOptionsFactory<CsrPluginOptions>, IPluginCapability>(
               Steps.Csr,
               defaultParam1: _settings.Csr.DefaultCsr,
               defaultBackends: new List<Type>() { typeof(Rsa), typeof(Ec) },
               shortDescription: "What kind of private key should be used for the certificate?",
               longDescription: "After ownership of the domain(s) has been proven, we will create a " +
                "Certificate Signing Request (CSR) to obtain the actual certificate. The CSR " +
                "determines properties of the certificate like which (type of) key to use. If you " +
                "are not sure what to pick here, RSA is the safe default.");
        }

        public async Task<PluginFrontend<PluginOptionsFactory<StorePluginOptions>, IPluginCapability>?> GetStorePlugin(IEnumerable<Plugin> chosen)
        {
            var defaultType = typeof(CertificateStore);
            var shortDescription = "How would you like to store the certificate?";
            var longDescription = "When we have the certificate, you can store in one or more ways to make it accessible " +
                        "to your applications. The Windows Certificate Store is the default location for IIS (unless you are " +
                        "managing a cluster of them).";
            if (chosen.Any())
            {
                if (!_runLevel.HasFlag(RunLevel.Advanced))
                {
                    return null;
                }
                longDescription = "";
                shortDescription = "Would you like to store it in another way too?";
                defaultType = typeof(StorePlugins.Null);
            }
            var defaultParam1 = _settings.Store.DefaultStore;
            if (!string.IsNullOrWhiteSpace(_arguments.Store))
            {
                defaultParam1 = _arguments.Store;
            }
            var csv = defaultParam1.ParseCsv();
            defaultParam1 = csv?.Count > chosen.Count() ? csv[chosen.Count()] : "";
            return await GetPlugin<PluginOptionsFactory<StorePluginOptions>, IPluginCapability>(
                Steps.Store,
                defaultParam1: defaultParam1,
                defaultBackends: new List<Type>() { defaultType, typeof(PfxFile) },
                shortDescription: shortDescription,
                longDescription: longDescription,
                allowAbort: false);
        }

        /// <summary>
        /// Allow user to choose a InstallationPlugins
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<PluginOptionsFactory<InstallationPluginOptions>, IInstallationPluginCapability>?> 
            GetInstallationPlugin(IEnumerable<Plugin> stores, IEnumerable<Plugin> installation)
        {
            var defaultType = typeof(InstallationPlugins.IIS);
            var shortDescription = "Which installation step should run first?";
            var longDescription = "With the certificate saved to the store(s) of your choice, " +
                "you may choose one or more steps to update your applications, e.g. to configure " +
                "the new thumbprint, or to update bindings.";
            if (installation.Any())
            {
                if (!_runLevel.HasFlag(RunLevel.Advanced))
                {
                    return null;
                }
                longDescription = "";
                shortDescription = "Add another installation step?";
                defaultType = typeof(InstallationPlugins.Null);
            }
            var defaultParam1 = _settings.Installation.DefaultInstallation;
            if (!string.IsNullOrWhiteSpace(_arguments.Installation))
            {
                defaultParam1 = _arguments.Installation;
            }
            var csv = defaultParam1.ParseCsv();
            defaultParam1 = csv?.Count > installation.Count() ? csv[installation.Count()] : "";
            return await GetPlugin<PluginOptionsFactory<InstallationPluginOptions>, IInstallationPluginCapability>(
                Steps.Installation,
                unusable: x => { var (a, b) = x.CanInstall(stores.Select(x => x.Backend), installation.Select(x => x.Backend)); return (!a, b); },
                defaultParam1: defaultParam1,
                defaultBackends: new List<Type>() { defaultType, typeof(InstallationPlugins.Null) },
                shortDescription: shortDescription,
                longDescription: longDescription,
                allowAbort: false);
        }
    }
}
