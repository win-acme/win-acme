using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
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
        private readonly PluginService _plugins;
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly RunLevel _runLevel;

        public InteractiveResolver(
            ILogService log,
            IInputService inputService,
            IArgumentsService arguments,
            PluginService pluginService,
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
        public override Task<ITargetPluginOptionsFactory> GetTargetPlugin(ILifetimeScope scope)
        {
            // List options for generating new certificates
            _input.Show(null, "Please specify how the list of domain names that will be included in the certificate " +
                "should be determined. If you choose for one of the \"all bindings\" options, the list will automatically be " +
                "updated for future renewals to reflect the bindings at that time.",
                true);
            var options = _plugins.TargetPluginFactories(scope).
                Where(x => !x.Hidden).
                OrderBy(x => x.Order).
                ThenBy(x => x.Description);

            var ret = _input.ChooseFromList("How shall we determine the domain(s) to include in the certificate?",
                _plugins.TargetPluginFactories(scope).Where(x => !x.Hidden),
                x => Choice.Create(x, description: x.Description),
                "Abort");
            return Task.FromResult(ret ?? new NullTargetFactory());
        }

        /// <summary>
        /// Allow user to choose a ValidationPlugin
        /// </summary>
        /// <returns></returns>
        public override Task<IValidationPluginOptionsFactory> GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            if (_runLevel.HasFlag(RunLevel.Advanced))
            {
                // List options for generating new certificates
                _input.Show(null, "The ACME server will need to verify that you are the owner of the domain names that you are requesting" +
                    " the certificate for. This happens both during initial setup *and* for every future renewal. There are two main methods of doing so: " +
                    "answering specific http requests (http-01) or create specific dns records (dns-01). For wildcard domains the latter is the only option. " +
                    "Various additional plugins are available from https://github.com/PKISharp/win-acme/.",
                    true);

                var ret = _input.ChooseFromList(
                    "How would you like prove ownership for the domain(s) in the certificate?",
                    _plugins.ValidationPluginFactories(scope).
                        Where(x => !(x is INull)).
                        Where(x => x.CanValidate(target)).
                        OrderByDescending(x => x.ChallengeType).
                        ThenBy(x => x.Order).
                        ThenBy(x => x.Description),
                    x => Choice.Create(x, description: $"[{x.ChallengeType}] {x.Description}", @default: x is SelfHostingOptionsFactory),
                    "Abort");
                return Task.FromResult(ret ?? new NullValidationFactory());
            }
            else
            {
                var ret = scope.Resolve<SelfHostingOptionsFactory>();
                if (ret.CanValidate(target))
                {
                    return Task.FromResult<IValidationPluginOptionsFactory>(ret);
                }
                else
                {
                    _log.Error("The default validation plugin cannot be " +
                        "used for this target. Most likely this is because " +
                        "you have included a wildcard identifier (*.example.com), " +
                        "which requires DNS validation. Choose another plugin " +
                        "from the advanced menu ('M').");
                    return Task.FromResult<IValidationPluginOptionsFactory>(new NullValidationFactory());
                }
            }
        }

        public override Task<ICsrPluginOptionsFactory> GetCsrPlugin(ILifetimeScope scope)
        {
            if (string.IsNullOrEmpty(_options.MainArguments.Csr) &&
                _runLevel.HasFlag(RunLevel.Advanced))
            {
                _input.Show(null, "After ownership of the domain(s) has been proven, we will create" +
                    " a Certificate Signing Request (CSR) to obtain the actual certificate. " +
                    "The CSR determines properties of the certificate like which " +
                    "(type of) key to use. If you are not sure what to pick here, RSA is the safe default.",
                    true);

                var ret = _input.ChooseFromList(
                    "What kind of private key should be used for the certificate?",
                    _plugins.CsrPluginOptionsFactories(scope).
                        Where(x => !(x is INull)).
                        OrderBy(x => x.Order).
                        ThenBy(x => x.Description),
                    x => Choice.Create(x, description: x.Description, @default: x is RsaOptionsFactory));
                return Task.FromResult(ret);
            }
            else
            {
                return base.GetCsrPlugin(scope);
            }
        }

        public override Task<IStorePluginOptionsFactory> GetStorePlugin(ILifetimeScope scope, IEnumerable<IStorePluginOptionsFactory> chosen)
        {
            if (string.IsNullOrEmpty(_options.MainArguments.Store) && _runLevel.HasFlag(RunLevel.Advanced))
            {
                var filtered = _plugins.
                    StorePluginFactories(scope).
                    Except(chosen).
                    OrderBy(x => x.Order).
                    ThenBy(x => x.Description).
                    ToList();

                if (filtered.Count() == 0)
                {
                    return Task.FromResult<IStorePluginOptionsFactory>(new NullStoreOptionsFactory());
                }

                if (chosen.Count() == 0)
                {
                    _input.Show(null, "When we have the certificate, you can store in one or more ways to make it accessible " +
                        "to your applications. The Windows Certificate Store is the default location for IIS (unless you are " +
                        "managing a cluster of them).",
                        true);
                }
                var question = "How would you like to store the certificate?";
                var @default = typeof(CertificateStoreOptionsFactory);
                if (chosen.Count() != 0)
                {
                    question = "Would you like to store it in another way too?";
                    @default = typeof(NullStoreOptionsFactory);
                    filtered.Add(new NullStoreOptionsFactory());
                }

                var store = _input.ChooseFromList(
                    question,
                    filtered,
                    x => Choice.Create(x, description: x.Description, @default: x.GetType() == @default),
                    "Abort");

                return Task.FromResult(store);
            }
            else
            {
                return base.GetStorePlugin(scope, chosen);
            }
        }

        /// <summary>
        /// Allow user to choose a InstallationPlugins
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override Task<IInstallationPluginOptionsFactory> GetInstallationPlugin(ILifetimeScope scope, IEnumerable<Type> storeTypes, IEnumerable<IInstallationPluginOptionsFactory> chosen)
        {
            var nullResult = Task.FromResult<IInstallationPluginOptionsFactory>(new NullInstallationOptionsFactory()); 
            if (_runLevel.HasFlag(RunLevel.Advanced))
            {
                var filtered = _plugins.
                    InstallationPluginFactories(scope).
                    Where(x => x.CanInstall(storeTypes)).
                    Except(chosen).
                    OrderBy(x => x.Order).
                    ThenBy(x => x.Description);

                if (filtered.Count() == 0)
                {
                    return nullResult;
                }

                if (filtered.Count() == 1 && filtered.First() is NullInstallationOptionsFactory)
                {
                    return nullResult;
                }

                if (chosen.Count() == 0)
                {
                    _input.Show(null, "With the certificate saved to the store(s) of your choice, you may choose one or more steps to update your applications, e.g. to configure the new thumbprint, or to update bindings.", true);
                }

                var question = "Which installation step should run first?";
                var @default =  filtered.OfType<IISWebOptionsFactory>().Any() ? 
                    typeof(IISWebOptionsFactory) : 
                    typeof(ScriptOptionsFactory);

                if (chosen.Count() != 0)
                {
                    question = "Add another installation step?";
                    @default = typeof(NullInstallationOptionsFactory);
                }

                var install = _input.ChooseFromList(
                    question,
                    filtered,
                    x => Choice.Create(x, description: x.Description, @default: x.GetType() == @default));

                return Task.FromResult(install);
            }
            else
            {
                if (chosen.Count() == 0)
                {
                    return Task.FromResult<IInstallationPluginOptionsFactory>(scope.Resolve<IISWebOptionsFactory>());
                }
                else
                {
                    return nullResult;
                }
            }
        }
    }
}
