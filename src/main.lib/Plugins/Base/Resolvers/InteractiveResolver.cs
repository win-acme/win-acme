using Autofac;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private readonly IAutofacBuilder _autofacBuilder;
        private readonly IInputService _input;
        private readonly RunLevel _runLevel;
        private readonly ILifetimeScope _scope;

        public InteractiveResolver(
            ILogService log,
            IInputService inputService,
            ISettingsService settings,
            MainArguments arguments,
            IPluginService pluginService,
            ILifetimeScope scope,
            IAutofacBuilder autofacBuilder,
            RunLevel runLevel)
        {
            _log = log;
            _settings = settings;
            _arguments = arguments;
            _input = inputService;
            _scope = scope;
            _runLevel = runLevel;
            _autofacBuilder = autofacBuilder;
            _plugins = pluginService;
        }

        [DebuggerDisplay("{Meta.Name}")]
        private class PluginChoice<TCapability, TOptions> 
            where TCapability : IPluginCapability
            where TOptions : PluginOptions, new()
        {
            public Plugin Meta { get; }
            public PluginFrontend<TCapability, TOptions> Frontend { get; }
            public State State { get; }
            public string Description { get; }
            public bool Default { get; set; }

            public PluginChoice(
                Plugin meta, 
                PluginFrontend<TCapability, TOptions> frontend,
                State state,
                string description,
                bool @default)
            {
                Meta = meta;
                Frontend = frontend;
                State = state;
                Description = description;
                Default = @default;
            }
        }

        private async Task<PluginFrontend<TCapability, TOptions>?> 
            GetPlugin<TCapability, TOptions>(
                Steps step,
                IEnumerable<Type> defaultBackends,
                string shortDescription,
                string longDescription,
                string? defaultParam1 = null,
                string? defaultParam2 = null,
                Func<PluginFrontend<TCapability, TOptions>, int>? sort = null, 
                Func<TCapability, State>? state = null,
                Func<PluginFrontend<TCapability, TOptions>, string>? description = null,
                bool allowAbort = true) 
                where TCapability : IPluginCapability
                where TOptions : PluginOptions, new()
        {
            // Helper method to determine final usability state
            // combination of plugin being enabled (e.g. due to missing
            // administrator rights) and being a right fit for the current
            // renewal (e.g. cannot validate wildcards using http-01)
            State combinedState(PluginFrontend<TCapability, TOptions> plugin)
            {
                var baseState = plugin.Capability.State;
                if (baseState.Disabled)
                {
                    return baseState;
                }
                else if (state != null)
                {
                    return state(plugin.Capability);
                }
                return State.EnabledState();
            };

            // Apply default sorting when no sorting has been provided yet
            var options = _plugins.
                GetPlugins(step).
                Where(x => !x.Hidden).
                Select(x => _autofacBuilder.PluginFrontend<TCapability, TOptions>(_scope, x)).
                Select(x => x.Resolve<PluginFrontend<TCapability, TOptions>>()).
                OrderBy(sort ??= x => 1).
                ThenBy(x => x.OptionsFactory.Order).
                ThenBy(x => x.Meta.Description).
                Select(plugin => new PluginChoice<TCapability, TOptions>(
                    plugin.Meta,
                    plugin,
                    combinedState(plugin),
                    description == null ? plugin.Meta.Description : description(plugin),
                    false)).
                ToList();
           
            // Default out when there are no reasonable plugins to pick
            if (!options.Any() || options.All(x => x.State.Disabled))
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

            var defaultOption = default(PluginChoice<TCapability, TOptions>);
            foreach (var backend in defaultBackends.Distinct())
            {
                defaultOption = options.FirstOrDefault(x => x.Frontend.Meta.Backend == backend);
                if (defaultOption == null)
                {
                    showMenu = true;
                    continue;
                }
                if (defaultOption.State.Disabled)
                {
                    _log.Warning("{n} plugin {x} not available: {m}",
                        char.ToUpper(className[0]) + className[1..],
                        defaultOption.Frontend.Meta.Name ?? backend.Name,
                        defaultOption.State.Reason);
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
                return defaultOption!.Frontend;
            }

            // List plugins for generating new certificates
            if (!string.IsNullOrEmpty(longDescription))
            {
                _input.CreateSpace();
                _input.Show(null, longDescription);
            }

            Choice<PluginFrontend<TCapability, TOptions>?> creator(PluginChoice<TCapability, TOptions> choice) {
                return Choice.Create<PluginFrontend<TCapability, TOptions>?>(
                       choice.Frontend,
                       description: choice.Description,
                       @default: choice.Default,
                       state: choice.State);
            }

            return allowAbort
                ? await _input.ChooseOptional(shortDescription, options, creator, "Abort")
                : await _input.ChooseRequired(shortDescription, options, creator);
        }

        /// <summary>
        /// Allow user to choose a TargetPlugin
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<IPluginCapability, TargetPluginOptions>?> GetTargetPlugin()
        {
            return await GetPlugin<IPluginCapability, TargetPluginOptions>(
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
        public async Task<PluginFrontend<IValidationPluginCapability, ValidationPluginOptions>?> GetValidationPlugin()
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
            return await GetPlugin<IValidationPluginCapability, ValidationPluginOptions>(
                Steps.Validation,
                sort: frontend =>
                {
                    return frontend.Capability.ChallengeType switch
                    {
                        Constants.Http01ChallengeType => 0,
                        Constants.Dns01ChallengeType => 1,
                        Constants.TlsAlpn01ChallengeType => 2,
                        _ => 3,
                    };
                },
                description: x => $"[{x.Capability.ChallengeType}] {x.Meta.Description}",
                defaultParam1: defaultParam1,
                defaultParam2: defaultParam2,
                defaultBackends: new List<Type>() { typeof(SelfHosting), typeof(FileSystem) },
                shortDescription: "How would you like prove ownership for the domain(s)?",
                longDescription: "The ACME server will need to verify that you are the owner of the domain names that you are requesting" +
                    " the certificate for. This happens both during initial setup *and* for every future renewal. There are two main methods of doing so: " +
                    "answering specific http requests (http-01) or create specific dns records (dns-01). For wildcard domains the latter is the only option. " +
                    "Various additional plugins are available from https://github.com/win-acme/win-acme/.");
        }

        public async Task<PluginFrontend<IPluginCapability, OrderPluginOptions>?> GetOrderPlugin()
        {
            return await GetPlugin<IPluginCapability, OrderPluginOptions>(
                   Steps.Order,
                   defaultParam1: _settings.Order.DefaultPlugin,
                   defaultBackends: new List<Type>() { typeof(Single) },
                   shortDescription: "Would you like to split this source into multiple certificates?",
                   longDescription: $"By default your source hosts are covered by a single certificate. " +
                        $"But if you want to avoid the {Constants.MaxNames} domain limit, want to prevent " +
                        $"information disclosure via the SAN list, and/or reduce the operational impact of " +
                        $"a single validation failure, you may choose to convert one source into multiple " +
                        $"certificates, using different strategies.");
        }

        public async Task<PluginFrontend<IPluginCapability, CsrPluginOptions>?> GetCsrPlugin()
        {
            return await GetPlugin<IPluginCapability, CsrPluginOptions>(
               Steps.Csr,
               defaultParam1: _settings.Csr.DefaultCsr,
               defaultBackends: new List<Type>() { typeof(Rsa), typeof(Ec) },
               shortDescription: "What kind of private key should be used for the certificate?",
               longDescription: "After ownership of the domain(s) has been proven, we will create a " +
                "Certificate Signing Request (CSR) to obtain the actual certificate. The CSR " +
                "determines properties of the certificate like which (type of) key to use. If you " +
                "are not sure what to pick here, RSA is the safe default.");
        }

        public async Task<PluginFrontend<IPluginCapability, StorePluginOptions>?> GetStorePlugin(IEnumerable<Plugin> chosen)
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
            return await GetPlugin<IPluginCapability, StorePluginOptions>(
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
        public async Task<PluginFrontend<IInstallationPluginCapability, InstallationPluginOptions>?> 
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
            return await GetPlugin<IInstallationPluginCapability, InstallationPluginOptions>(
                Steps.Installation,
                state: x => x.CanInstall(stores.Select(x => x.Backend), installation.Select(x => x.Backend)),
                defaultParam1: defaultParam1,
                defaultBackends: new List<Type>() { defaultType, typeof(InstallationPlugins.Null) },
                shortDescription: shortDescription,
                longDescription: longDescription,
                allowAbort: false);
        }
    }
}
