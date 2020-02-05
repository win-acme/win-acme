using Autofac;
using PKISharp.WACS.DomainObjects;
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
            IArgumentsService arguments,
            IPluginService pluginService,
            RunLevel runLevel)
            : base(log, arguments, pluginService)
        {
            _log = log;
            _input = inputService;
            _plugins = pluginService;
            _runLevel = runLevel;
        }

        /// <summary>
        /// Allow user to choose a TargetPlugin
        /// </summary>
        /// <returns></returns>
        public override async Task<ITargetPluginOptionsFactory?> GetTargetPlugin(ILifetimeScope scope)
        {
            var options = _plugins.TargetPluginFactories(scope).
                Where(x => !x.Hidden).
                OrderBy(x => x.Order).
                ThenBy(x => x.Description);

            var defaultType = typeof(IISOptionsFactory);
            if (!options.OfType<IISOptionsFactory>().Any(x => !x.Disabled.Item1))
            {
                defaultType = typeof(ManualOptionsFactory);
            }

            if (!_runLevel.HasFlag(RunLevel.Advanced))
            {
                return (ITargetPluginOptionsFactory)scope.Resolve(defaultType);
            }

            // List options for generating new certificates
            _input.Show(null, "Please specify how the list of domain names that will be included in the certificate " +
            "should be determined. If you choose for one of the \"all bindings\" options, the list will automatically be " +
            "updated for future renewals to reflect the bindings at that time.",
            true);

            var ret = await _input.ChooseOptional(
                "How shall we determine the domain(s) to include in the certificate?",
                options,
                x => Choice.Create<ITargetPluginOptionsFactory?>(
                    x,
                    description: x.Description,
                    @default: x.GetType() == defaultType,
                    disabled: x.Disabled.Item1,
                    disabledReason: x.Disabled.Item2), 
                "Abort");

            return ret ?? new NullTargetFactory();
        }

        /// <summary>
        /// Allow user to choose a ValidationPlugin
        /// </summary>
        /// <returns></returns>
        public override async Task<IValidationPluginOptionsFactory?> GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            if (_runLevel.HasFlag(RunLevel.Advanced))
            {
                // List options for generating new certificates
                _input.Show(null, "The ACME server will need to verify that you are the owner of the domain names that you are requesting" +
                    " the certificate for. This happens both during initial setup *and* for every future renewal. There are two main methods of doing so: " +
                    "answering specific http requests (http-01) or create specific dns records (dns-01). For wildcard domains the latter is the only option. " +
                    "Various additional plugins are available from https://github.com/PKISharp/win-acme/.",
                    true);

                var options = _plugins.ValidationPluginFactories(scope).
                        Where(x => !(x is INull)).
                        Where(x => x.CanValidate(target)).
                        OrderBy(x => {
                            return x.ChallengeType switch
                            {
                                Constants.Http01ChallengeType => 0,
                                Constants.Dns01ChallengeType => 1,
                                Constants.TlsAlpn01ChallengeType => 2,
                                _ => 3,
                            };
                        }).
                        ThenBy(x => x.Order).
                        ThenBy(x => x.Description);

                var defaultType = typeof(SelfHostingOptionsFactory);
                if (!options.OfType<SelfHostingOptionsFactory>().Any(x => !x.Disabled.Item1))
                {
                    defaultType = typeof(FileSystemOptionsFactory);
                }
                var ret = await _input.ChooseOptional(
                    "How would you like prove ownership for the domain(s) in the certificate?",
                    options,
                    x => Choice.Create<IValidationPluginOptionsFactory?>(
                        x, 
                        description: $"[{x.ChallengeType}] {x.Description}", 
                        @default: x.GetType() == defaultType,
                        disabled: x.Disabled.Item1,
                        disabledReason: x.Disabled.Item2),
                    "Abort");
                return ret ?? new NullValidationFactory();
            }
            else
            {
                var ret = scope.Resolve<SelfHostingOptionsFactory>();
                if (ret.CanValidate(target))
                {
                    return ret;
                }
                else
                {
                    _log.Error("The default validation plugin cannot be " +
                        "used for this target. Most likely this is because " +
                        "you have included a wildcard identifier (*.example.com), " +
                        "which requires DNS validation. Choose another plugin " +
                        "from the advanced menu ('M').");
                    return new NullValidationFactory();
                }
            }
        }

        public override async Task<ICsrPluginOptionsFactory?> GetCsrPlugin(ILifetimeScope scope)
        {
            if (string.IsNullOrEmpty(_options.MainArguments.Csr) &&
                _runLevel.HasFlag(RunLevel.Advanced))
            {
                _input.Show(null, "After ownership of the domain(s) has been proven, we will create" +
                    " a Certificate Signing Request (CSR) to obtain the actual certificate. " +
                    "The CSR determines properties of the certificate like which " +
                    "(type of) key to use. If you are not sure what to pick here, RSA is the safe default.",
                    true);

                var ret = await _input.ChooseRequired(
                    "What kind of private key should be used for the certificate?",
                    _plugins.CsrPluginOptionsFactories(scope).
                        Where(x => !(x is INull)).
                        OrderBy(x => x.Order).
                        ThenBy(x => x.Description),
                    x => Choice.Create(
                        x, 
                        description: x.Description, 
                        @default: x is RsaOptionsFactory,
                        disabled: x.Disabled.Item1,
                        disabledReason: x.Disabled.Item2));
                return ret;
            }
            else
            {
                return await base.GetCsrPlugin(scope);
            }
        }

        public override async Task<IStorePluginOptionsFactory?> GetStorePlugin(ILifetimeScope scope, IEnumerable<IStorePluginOptionsFactory> chosen)
        {
            if (string.IsNullOrEmpty(_options.MainArguments.Store) && _runLevel.HasFlag(RunLevel.Advanced))
            {
                var filtered = _plugins.
                    StorePluginFactories(scope).
                    Except(chosen).
                    OrderBy(x => x.Order).
                    ThenBy(x => x.Description).
                    ToList();

                if (filtered.Where(x => !x.Disabled.Item1).Count() == 0)
                {
                    return new NullStoreOptionsFactory();
                }

                if (chosen.Count() == 0)
                {
                    _input.Show(null, "When we have the certificate, you can store in one or more ways to make it accessible " +
                        "to your applications. The Windows Certificate Store is the default location for IIS (unless you are " +
                        "managing a cluster of them).",
                        true);
                }
                var question = "How would you like to store the certificate?";
                var defaultType = typeof(CertificateStoreOptionsFactory);
                if (!filtered.OfType<CertificateStoreOptionsFactory>().Any(x => !x.Disabled.Item1))
                {
                    defaultType = typeof(PemFilesOptionsFactory);
                }

                if (chosen.Count() != 0)
                {
                    question = "Would you like to store it in another way too?";
                    defaultType = typeof(NullStoreOptionsFactory);
                }

                var store = await _input.ChooseOptional(
                    question,
                    filtered,
                    x => Choice.Create<IStorePluginOptionsFactory?>(
                        x, 
                        description: x.Description,
                        @default: x.GetType() == defaultType,
                        disabled: x.Disabled.Item1,
                        disabledReason: x.Disabled.Item2),
                    "Abort");

                return store;
            }
            else
            {
                return await base.GetStorePlugin(scope, chosen);
            }
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
            if (_runLevel.HasFlag(RunLevel.Advanced))
            {
                var filtered = _plugins.
                    InstallationPluginFactories(scope).
                    Except(chosen).
                    OrderBy(x => x.Order).
                    ThenBy(x => x.Description).
                    Select(x => new {
                        plugin = x, 
                        usable = !x.Disabled.Item1 && x.CanInstall(storeTypes) 
                    }).
                    ToList();

                var usable = filtered.Where(x => x.usable);
                if (usable.Count() == 0)
                {
                    return new NullInstallationOptionsFactory();
                }

                if (usable.Count() == 1 && usable.First().plugin is NullInstallationOptionsFactory)
                {
                    return new NullInstallationOptionsFactory();
                }

                if (chosen.Count() == 0)
                {
                    _input.Show(null, "With the certificate saved to the store(s) of your choice, you may choose one or more steps to update your applications, e.g. to configure the new thumbprint, or to update bindings.", true);
                }

                var question = "Which installation step should run first?";
                var @default = usable.Any(x => x.plugin is IISWebOptionsFactory) ? 
                    typeof(IISWebOptionsFactory) : 
                    typeof(NullInstallationOptionsFactory);

                if (chosen.Count() != 0)
                {
                    question = "Add another installation step?";
                    @default = typeof(NullInstallationOptionsFactory);
                }

                var install = await _input.ChooseRequired(
                    question,
                    filtered,
                    x => Choice.Create(
                        x,
                        description: x.plugin.Description,
                        disabled: !x.usable,
                        disabledReason: x.plugin.Disabled.Item1 ? x.plugin.Disabled.Item2 : "Incompatible with selected store.",
                        @default: x.plugin.GetType() == @default)) ;

                return install.plugin;
            }
            else
            {
                if (chosen.Count() == 0)
                {
                    return scope.Resolve<IISWebOptionsFactory>();
                }
                else
                {
                    return new NullInstallationOptionsFactory();
                }
            }
        }
    }
}
