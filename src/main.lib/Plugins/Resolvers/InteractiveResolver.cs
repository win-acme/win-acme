using Autofac;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Resolvers
{
    public class InteractiveResolver : UnattendedResolver
    {
        private readonly IPluginService _plugins;
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly RunLevel _runLevel;

        public InteractiveResolver(
            ILogService log,
            IInputService inputService,
            ISettingsService settings,
            MainArguments arguments,
            IPluginService pluginService,
            RunLevel runLevel)
            : base(log, settings, arguments, pluginService)
        {
            _log = log;
            _input = inputService;
            _plugins = pluginService;
            _runLevel = runLevel;
        }

        private async Task<T> GetPlugin<T>(
            ILifetimeScope scope,
            Type defaultType,
            Type defaultTypeFallback,
            T nullResult,
            string className,
            string shortDescription,
            string longDescription,
            string? defaultParam1 = null,
            string? defaultParam2 = null,
            Func<IEnumerable<T>, IEnumerable<T>>? sort = null,
            Func<IEnumerable<T>, IEnumerable<T>>? filter = null,
            Func<T, (bool, string?)>? unusable = null,
            Func<T, string>? description = null,
            bool allowAbort = true) where T : IPluginOptionsFactory
        {
            // Helper method to determine final usability state
            // combination of plugin being enabled (e.g. due to missing
            // administrator rights) and being a right fit for the current
            // renewal (e.g. cannot validate wildcards using http-01)
            (bool, string?) disabledOrUnusable(T plugin)
            {
                var disabled = plugin.Disabled;
                if (disabled.Item1)
                {
                    return disabled;
                }
                else if (unusable != null)
                {
                    return unusable(plugin);
                }
                return (false, null);
            };

            // Apply default sorting when no sorting has been provided yet
            var options = _plugins.GetFactories<T>(scope);
            options = filter != null ? filter(options) : options.Where(x => !(x is INull));
            options = sort != null ? sort(options) : options.OrderBy(x => x.Order).ThenBy(x => x.Description);

            var localOptions = options.
                Select(x => new {
                    plugin = x, 
                    type = x.GetType(),
                    disabled = disabledOrUnusable(x) 
                });

            // Default out when there are no reasonable options to pick
            if (!localOptions.Any() || 
                localOptions.All(x => x.disabled.Item1) || 
                localOptions.All(x => x.plugin is INull))
            {
                return nullResult;
            }

            // Always show the menu in advanced mode, only when no default
            // selection can be made in simple mode
            var showMenu = _runLevel.HasFlag(RunLevel.Advanced);
            if (!string.IsNullOrEmpty(defaultParam1))
            {
                var defaultPlugin = _plugins.GetFactory<T>(scope, defaultParam1, defaultParam2);
                if (defaultPlugin == null)
                {
                    _log.Error("Unable to find {n} plugin {p}", className, defaultParam1);
                    showMenu = true;
                } 
                else
                {
                    defaultType = defaultPlugin.GetType();
                }
            }

            var defaultOption = localOptions.First(x => x.type == defaultType);
            var defaultTypeDisabled = defaultOption.disabled;
            if (defaultTypeDisabled.Item1)
            {
                _log.Warning("{n} plugin {x} not available: {m}",
                    char.ToUpper(className[0]) + className.Substring(1),
                    defaultOption.plugin.Name, 
                    defaultTypeDisabled.Item2);
                defaultType = defaultTypeFallback;
                showMenu = true;
            }

            if (!showMenu)
            {
                return (T)scope.Resolve(defaultType);
            }

            // List options for generating new certificates
            if (!string.IsNullOrEmpty(longDescription))
            {
                _input.CreateSpace();
                _input.Show(null, longDescription);
            }

            Choice<IPluginOptionsFactory?> creator(T plugin, Type type, (bool, string?) disabled) {
                return Choice.Create<IPluginOptionsFactory?>(
                       plugin,
                       description: description == null ? plugin.Description : description(plugin),
                       @default: type == defaultType && !disabled.Item1,
                       disabled: disabled);
            }

            var ret = default(T);
            if (allowAbort)
            {
                ret = (T?)await _input.ChooseOptional(
                    shortDescription,
                    localOptions,
                    x => creator(x.plugin, x.type, x.disabled),
                    "Abort");
            } 
            else
            {
                ret = (T?)await _input.ChooseRequired(
                    shortDescription,
                    localOptions,
                    x => creator(x.plugin, x.type, x.disabled));
            }
            return ret ?? nullResult;
        }

        /// <summary>
        /// Allow user to choose a TargetPlugin
        /// </summary>
        /// <returns></returns>
        public override async Task<ITargetPluginOptionsFactory> GetTargetPlugin(ILifetimeScope scope)
        {
            return await GetPlugin<ITargetPluginOptionsFactory>(
                scope,
                defaultParam1: _settings.Target.DefaultTarget,
                defaultType: typeof(IISOptionsFactory),
                defaultTypeFallback: typeof(ManualOptionsFactory),
                nullResult: new NullTargetFactory(),
                className: "target",
                shortDescription: "How shall we determine the domain(s) to include in the certificate?",
                longDescription: "Please specify how the list of domain names that will be included in the certificate " +
                    "should be determined. If you choose for one of the \"all bindings\" options, the list will automatically be " +
                    "updated for future renewals to reflect the bindings at that time.");
        }

        /// <summary>
        /// Allow user to choose a ValidationPlugin
        /// </summary>
        /// <returns></returns>
        public override async Task<IValidationPluginOptionsFactory> GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            var defaultParam1 = _settings.Validation.DefaultValidation;
            var defaultParam2 = _settings.Validation.DefaultValidationMode ?? Constants.Http01ChallengeType;
            if (!string.IsNullOrWhiteSpace(_arguments.Validation))
            {
                defaultParam1 = _arguments.Validation;
            }
            if (!string.IsNullOrWhiteSpace(_arguments.ValidationMode))
            {
                defaultParam2 = _arguments.ValidationMode;
            }
            return await GetPlugin<IValidationPluginOptionsFactory>(
                scope,
                sort: x =>
                    x.
                        OrderBy(x =>
                        {
                            return x.ChallengeType switch
                            {
                                Constants.Http01ChallengeType => 0,
                                Constants.Dns01ChallengeType => 1,
                                Constants.TlsAlpn01ChallengeType => 2,
                                _ => 3,
                            };
                        }).
                        ThenBy(x => x.Order).
                        ThenBy(x => x.Description),
                unusable: x => (!x.CanValidate(target), "Unsuppored target. Most likely this is because you have included a wildcard identifier (*.example.com), which requires DNS validation."),
                description: x => $"[{x.ChallengeType}] {x.Description}",
                defaultParam1: defaultParam1,
                defaultParam2: defaultParam2,
                defaultType: typeof(SelfHostingOptionsFactory),
                defaultTypeFallback: typeof(FileSystemOptionsFactory),
                nullResult: new NullValidationFactory(),
                className: "validation",
                shortDescription: "How would you like prove ownership for the domain(s)?",
                longDescription: "The ACME server will need to verify that you are the owner of the domain names that you are requesting" +
                    " the certificate for. This happens both during initial setup *and* for every future renewal. There are two main methods of doing so: " +
                    "answering specific http requests (http-01) or create specific dns records (dns-01). For wildcard domains the latter is the only option. " +
                    "Various additional plugins are available from https://github.com/win-acme/win-acme/.");
        }

        public override async Task<ICsrPluginOptionsFactory> GetCsrPlugin(ILifetimeScope scope)
        {
            return await GetPlugin<ICsrPluginOptionsFactory>(
               scope,
               defaultParam1: _settings.Csr.DefaultCsr,
               defaultType: typeof(RsaOptionsFactory),
               defaultTypeFallback: typeof(EcOptionsFactory),
               nullResult: new NullCsrFactory(),
               className: "csr",
               shortDescription: "What kind of private key should be used for the certificate?",
               longDescription: "After ownership of the domain(s) has been proven, we will create a " +
                "Certificate Signing Request (CSR) to obtain the actual certificate. The CSR " +
                "determines properties of the certificate like which (type of) key to use. If you " +
                "are not sure what to pick here, RSA is the safe default.");
        }

        public override async Task<IStorePluginOptionsFactory?> GetStorePlugin(ILifetimeScope scope, IEnumerable<IStorePluginOptionsFactory> chosen)
        {
            var defaultType = typeof(CertificateStoreOptionsFactory);
            var shortDescription = "How would you like to store the certificate?";
            var longDescription = "When we have the certificate, you can store in one or more ways to make it accessible " +
                        "to your applications. The Windows Certificate Store is the default location for IIS (unless you are " +
                        "managing a cluster of them).";
            if (chosen.Count() != 0)
            {
                if (!_runLevel.HasFlag(RunLevel.Advanced))
                {
                    return new NullStoreOptionsFactory();
                }
                longDescription = "";
                shortDescription = "Would you like to store it in another way too?";
                defaultType = typeof(NullStoreOptionsFactory);
            }
            var defaultParam1 = _settings.Store.DefaultStore;
            if (!string.IsNullOrWhiteSpace(_arguments.Store))
            {
                defaultParam1 = _arguments.Store;
            }
            var csv = defaultParam1.ParseCsv();
            defaultParam1 = csv?.Count > chosen.Count() ? 
                csv[chosen.Count()] : 
                "";
            return await GetPlugin<IStorePluginOptionsFactory>(
                scope,
                filter: (x) => x, // Disable default null check
                defaultParam1: defaultParam1,
                defaultType: defaultType,
                defaultTypeFallback: typeof(PemFilesOptionsFactory),
                nullResult: new NullStoreOptionsFactory(),
                className: "store",
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
        public override async Task<IInstallationPluginOptionsFactory?> GetInstallationPlugin(ILifetimeScope scope, IEnumerable<Type> storeTypes, IEnumerable<IInstallationPluginOptionsFactory> chosen)
        {
            var defaultType = typeof(IISWebOptionsFactory);
            var shortDescription = "Which installation step should run first?";
            var longDescription = "With the certificate saved to the store(s) of your choice, " +
                "you may choose one or more steps to update your applications, e.g. to configure " +
                "the new thumbprint, or to update bindings.";
            if (chosen.Count() != 0)
            {
                if (!_runLevel.HasFlag(RunLevel.Advanced))
                {
                    return new NullInstallationOptionsFactory();
                }
                longDescription = "";
                shortDescription = "Add another installation step?";
                defaultType = typeof(NullInstallationOptionsFactory);
            }
            var defaultParam1 = _settings.Installation.DefaultInstallation;
            if (!string.IsNullOrWhiteSpace(_arguments.Installation))
            {
                defaultParam1 = _arguments.Installation;
            }
            var csv = defaultParam1.ParseCsv();
            defaultParam1 = csv?.Count > chosen.Count() ?
                csv[chosen.Count()] :
                "";
            return await GetPlugin<IInstallationPluginOptionsFactory>(
                scope,
                filter: (x) => x, // Disable default null check
                unusable: x => (!x.CanInstall(storeTypes), "This step cannot be used in combination with the specified store(s)"),
                defaultParam1: defaultParam1,
                defaultType: defaultType,
                defaultTypeFallback: typeof(NullInstallationOptionsFactory),
                nullResult: new NullInstallationOptionsFactory(),
                className: "installation",
                shortDescription: shortDescription,
                longDescription: longDescription,
                allowAbort: false);
        }
    }
}
