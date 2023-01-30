using Autofac;
using Microsoft.Win32;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Host.Services.Legacy;
using PKISharp.WACS.Plugins;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.OrderPlugins;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services.Legacy;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Services
{
    internal class AutofacBuilder : IAutofacBuilder
    {
        private readonly ILogService _log;
        private readonly IPluginService _plugins;

        public AutofacBuilder(ILogService log, IPluginService plugins)
        {
            _plugins = plugins;
            _log = log;
        }

        /// <summary>
        /// This is used to import renewals from 1.9.x
        /// </summary>
        /// <param name="main"></param>
        /// <param name="fromUri"></param>
        /// <param name="toUri"></param>
        /// <returns></returns>
        public ILifetimeScope Legacy(ILifetimeScope main, Uri fromUri, Uri toUri)
        {
            return main.BeginLifetimeScope(builder =>
            {
                var realSettings = main.Resolve<ISettingsService>();
                var realArguments = main.Resolve<MainArguments>();
   
                builder.Register(c => new MainArguments { 
                        BaseUri = fromUri.ToString()
                    }).
                    As<MainArguments>().
                    SingleInstance();

                builder.RegisterType<LegacySettingsService>().
                    WithParameter(new TypedParameter(typeof(ISettingsService), realSettings)).
                    SingleInstance();

                builder.RegisterType<LegacyTaskSchedulerService>();

                builder.RegisterType<TaskSchedulerService>().
                    WithParameter(new TypedParameter(typeof(MainArguments), realArguments)).
                    WithParameter(new TypedParameter(typeof(ISettingsService), realSettings)).
                    SingleInstance();

                builder.Register((scope) => main.Resolve<IRenewalStore>()).
                    As<IRenewalStore>().
                    SingleInstance();

                // Check where to load Renewals from
                var hive = Registry.CurrentUser;
                var key = hive.OpenSubKey($"Software\\letsencrypt-win-simple");
                if (key == null)
                {
                    hive = Registry.LocalMachine;
                    key = hive.OpenSubKey($"Software\\letsencrypt-win-simple");
                }
                var log = main.Resolve<ILogService>();
                if (key != null)
                {
                    log.Debug("Loading legacy renewals from registry hive {name}", hive.Name);
                    builder.RegisterType<RegistryLegacyRenewalService>().
                            As<ILegacyRenewalService>().
                            WithParameter(new NamedParameter("hive", hive.Name)).
                            SingleInstance();
                }
                else
                {
                    log.Debug("Loading legacy renewals from file");
                    builder.RegisterType<FileLegacyRenewalService>().
                        As<ILegacyRenewalService>().
                        SingleInstance();
                }

                builder.RegisterType<Importer>().
                    SingleInstance();
            });
        }

        /// <summary>
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope Execution(ILifetimeScope main, Renewal renewal, RunLevel runLevel)
        {
            _log.Verbose("Autofac: creating {name} scope with parent {tag}", nameof(Execution), main.Tag);
            var ret = main.BeginLifetimeScope(nameof(Execution), builder =>
            {
                builder.Register(c => runLevel).As<RunLevel>();
                builder.RegisterType<FindPrivateKey>().SingleInstance();
                builder.RegisterInstance(renewal);
            });
            ret = PluginBackend<ITargetPlugin, TargetPluginOptions>(ret, renewal.TargetPluginOptions, "target");
            return ret;
        }

        /// <summary>
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope Split(ILifetimeScope execution, Target target)
        {
            _log.Verbose("Autofac: creating {name} scope with parent {tag}", nameof(Split), execution.Tag);
            var ret = execution.BeginLifetimeScope(nameof(Split), builder => builder.RegisterInstance(target));
            ret = PluginBackend<IOrderPlugin, OrderPluginOptions>(ret, execution.Resolve<Renewal>().OrderPluginOptions ?? new SingleOptions(), "order");
            return ret;
        }

        /// <summary>
        /// For a single sub target within the execution, each order has its own instance of the CSR plugins
        /// so that differrent certificates can/will use different private keys 
        /// </summary>
        /// <param name="main"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public ILifetimeScope Order(ILifetimeScope execution, Order order)
        {
            _log.Verbose("Autofac: creating {name} scope with parent {tag}", nameof(Order), execution.Tag);
            var ret = execution.BeginLifetimeScope($"order-{order.CacheKeyPart ?? "main"}", builder => 
                builder.RegisterInstance(order.Target));
            ret = PluginBackend<ICsrPlugin, CsrPluginOptions>(ret, order.Renewal.CsrPluginOptions ?? new RsaOptions(), "csr");
            return ret;
        }

        /// <summary>
        /// For a single validation target
        /// </summary>
        /// <param name="main"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public ILifetimeScope Target(ILifetimeScope execution, Target target)
        {
            _log.Verbose("Autofac: creating {name} scope with parent {tag}", nameof(Target), execution.Tag);
            return execution.BeginLifetimeScope($"target", builder => builder.RegisterInstance(target));
        }

        /// <summary>
        /// Shortcut for backends using the IPluginCapability
        /// </summary>
        /// <typeparam name="TBackend"></typeparam>
        /// <typeparam name="TOptions"></typeparam>
        /// <param name="execution"></param>
        /// <param name="options"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public ILifetimeScope PluginBackend<TBackend, TOptions>(ILifetimeScope execution, TOptions options, string key) 
            where TBackend : IPlugin
            where TOptions : PluginOptions
            => PluginBackend<TBackend, IPluginCapability, TOptions>(execution, options, key);

        /// <summary>
        /// For a single plugin step within the renewal
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope PluginBackend<TBackend, TCapability, TOptions>(ILifetimeScope execution, TOptions options, string key)
            where TBackend : IPlugin
            where TCapability : IPluginCapability
            where TOptions : PluginOptions
        {
            _log.Verbose("Autofac: creating {name}<{backend}> scope with parent {tag}", nameof(PluginBackend), typeof(TBackend).Name, execution.Tag);
            if (!_plugins.TryGetPlugin(options, out var plugin)) 
            {
                throw new Exception($"Unknown {typeof(TBackend).Name} plugin {options.Plugin}");
            }
            if (!plugin.Backend.IsAssignableTo(typeof(TBackend))) 
            {
                throw new Exception($"{plugin.Backend.Name} is not a {typeof(TBackend).Name}");
            }
            if (!plugin.Capability.IsAssignableTo(typeof(TCapability)))
            {
                throw new Exception($"{plugin.Capability.Name} is not a {typeof(TCapability).Name}");
            }
            if (!plugin.Options.IsAssignableTo(typeof(TOptions)))
            {
                throw new Exception($"{plugin.Options.Name} is not a {typeof(TOptions).Name}");
            }
            return execution.BeginLifetimeScope($"{nameof(PluginBackend)}<{typeof(TBackend).Name}>",
                builder => {
                    builder.RegisterInstance(plugin).As<Plugin>().Named<Plugin>(key);
                    builder.RegisterInstance(options).As<TOptions>().As(options.GetType()).As(options.GetType().BaseType ?? options.GetType()); 
                    builder.RegisterType(plugin.Backend).As<TBackend>().SingleInstance();
                    builder.RegisterType(plugin.Capability).As<TCapability>().SingleInstance().Named<TCapability>(key);
                    builder.Register(c => new PluginBackend<TBackend, TCapability, TOptions>(plugin, c.Resolve<TBackend>(), c.ResolveNamed<TCapability>(key), options)).As<PluginBackend<TBackend, TCapability, TOptions>>();
                });
        }

        /// <summary>
        /// For a single plugin step within the renewal
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope PluginFrontend<TCapability, TOptions>(ILifetimeScope execution, Plugin plugin)
            where TCapability : IPluginCapability
            where TOptions : PluginOptions, new()
        {
            _log.Verbose("Autofac: creating {name}<{backend}> scope with parent {tag}", nameof(PluginFrontend), typeof(TOptions).Name, execution.Tag);
            if (!plugin.Capability.IsAssignableTo(typeof(TCapability)))
            {
                throw new Exception($"{plugin.Capability.Name} is not a {typeof(TCapability).Name}");
            }
            var genericFactory = typeof(IPluginOptionsFactory<>).MakeGenericType(plugin.Options);
            if (!plugin.OptionsFactory.IsAssignableTo(genericFactory))
            {
                throw new Exception($"{plugin.OptionsFactory.Name} is not a {genericFactory.Name}");
            }
            return execution.BeginLifetimeScope(
                $"{nameof(PluginFrontend)}<{typeof(TOptions).Name}>",
                builder => {
                    // So that we don't create a new IIS client for every single plugin
                    builder.RegisterInstance(execution.Resolve<IIISClient>());
                    builder.RegisterInstance(plugin).As<Plugin>();
                    builder.RegisterType(plugin.OptionsFactory).As<IPluginOptionsFactory<TOptions>>().As(genericFactory).SingleInstance();
                    builder.RegisterType(plugin.Capability).As<TCapability>().SingleInstance();
                    builder.Register(c => new PluginFrontend<TCapability, TOptions>(plugin, c.Resolve<IPluginOptionsFactory<TOptions>>(), c.Resolve<TCapability>())).As<PluginFrontend<TCapability, TOptions>>();
                });
        }

        public PluginFrontend<IValidationPluginCapability, ValidationPluginOptions> 
            ValidationFrontend(ILifetimeScope execution, ValidationPluginOptions options, Identifier identifier)
        {
            var dummyTarget = new Target(identifier);
            var dummyScope = Target(execution, dummyTarget);
            var plugin = _plugins.GetPlugin(options);
            var pluginHelper = PluginFrontend<IValidationPluginCapability, ValidationPluginOptions>(dummyScope, plugin);
            return pluginHelper.Resolve<PluginFrontend<IValidationPluginCapability, ValidationPluginOptions>>();
        }
    }
}
