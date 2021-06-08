using Autofac;
using Microsoft.Win32;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Host.Services.Legacy;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.ValidationPlugins;
using PKISharp.WACS.Services.Legacy;
using System;
using System.Linq;

namespace PKISharp.WACS.Services
{
    internal class AutofacBuilder : IAutofacBuilder
    {
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
        /// For revocation and configuration
        /// </summary>
        /// <param name="main"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope Configuration(ILifetimeScope main, Renewal renewal, RunLevel runLevel)
        {
            var resolver = runLevel.HasFlag(RunLevel.Interactive)
                ? main.Resolve<InteractiveResolver>(new TypedParameter(typeof(RunLevel), runLevel))
                : (IResolver)main.Resolve<UnattendedResolver>();
            return main.BeginLifetimeScope(builder =>
            {
                builder.Register(c => runLevel).As<RunLevel>();
                builder.Register(c => resolver).As<IResolver>();
                builder.Register(c => resolver.GetTargetPlugin(main).Result).As<ITargetPluginOptionsFactory>().SingleInstance();
                builder.Register(c => resolver.GetCsrPlugin(main).Result).As<ICsrPluginOptionsFactory>().SingleInstance();
            });
        }

        /// <summary>
        /// For configuration and renewal
        /// </summary>
        /// <param name="main"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope Target(ILifetimeScope main, Renewal renewal, RunLevel runLevel)
        {
            var resolver = runLevel.HasFlag(RunLevel.Interactive)
                ? main.Resolve<InteractiveResolver>(new TypedParameter(typeof(RunLevel), runLevel))
                : (IResolver)main.Resolve<UnattendedResolver>();
            return main.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(renewal.TargetPluginOptions).As(renewal.TargetPluginOptions.GetType());
                builder.RegisterInstance(renewal.TargetPluginOptions).As(renewal.TargetPluginOptions.GetType().BaseType!);
                builder.RegisterType(renewal.TargetPluginOptions.Instance).As<ITargetPlugin>().SingleInstance();
                builder.Register(c => c.Resolve<ITargetPlugin>().Generate().Result).As<Target>().SingleInstance();
                builder.Register(c => resolver.GetValidationPlugin(main, c.Resolve<Target>()).Result).As<IValidationPluginOptionsFactory>().SingleInstance();
                builder.Register(c => resolver.GetOrderPlugin(main, c.Resolve<Target>()).Result).As<IOrderPluginOptionsFactory>().SingleInstance();
            });
        }

        /// <summary>
        /// For renewal and creating scheduled task 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="renewal"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        public ILifetimeScope Execution(ILifetimeScope target, Renewal renewal, RunLevel runLevel)
        {
            return target.BeginLifetimeScope(builder =>
            {
                builder.Register(c => runLevel).As<RunLevel>();
                builder.RegisterType<FindPrivateKey>().SingleInstance();

                // Used to configure TaskScheduler without renewal
                if (renewal != null)
                {
                    builder.RegisterInstance(renewal);

                    builder.RegisterInstance(renewal.StorePluginOptions).As(renewal.StorePluginOptions.GetType());
                    if (renewal.CsrPluginOptions != null)
                    {
                        builder.RegisterInstance(renewal.CsrPluginOptions).As(renewal.CsrPluginOptions.GetType());
                    }
                    if (renewal.OrderPluginOptions != null)
                    {
                        builder.RegisterInstance(renewal.OrderPluginOptions).As(renewal.OrderPluginOptions.GetType());
                    }
                    builder.RegisterInstance(renewal.ValidationPluginOptions).As(renewal.ValidationPluginOptions.GetType());
                    builder.RegisterInstance(renewal.TargetPluginOptions).As(renewal.TargetPluginOptions.GetType());

                    // Find factory based on options
                    builder.Register(x =>
                    {
                        var plugin = x.Resolve<IPluginService>();
                        var match = plugin.GetFactories<IValidationPluginOptionsFactory>(target).First(vp => vp.OptionsType.PluginId() == renewal.ValidationPluginOptions.Plugin);
                        if (match == null)
                        {
                            return new NullValidationFactory();
                        }
                        return match;
                    }).As<IValidationPluginOptionsFactory>().SingleInstance();

                    if (renewal.CsrPluginOptions != null)
                    {
                        builder.RegisterType(renewal.CsrPluginOptions.Instance).As<ICsrPlugin>().SingleInstance();
                    }
                    if (renewal.OrderPluginOptions != null)
                    {
                        builder.RegisterType(renewal.OrderPluginOptions.Instance).As<IOrderPlugin>().SingleInstance();
                    } 
                    else
                    {
                        builder.RegisterType<Plugins.OrderPlugins.Single>().As<IOrderPlugin>().SingleInstance();
                    }
                    builder.RegisterType(renewal.ValidationPluginOptions.Instance).As<IValidationPlugin>().SingleInstance();
                    builder.RegisterType(renewal.TargetPluginOptions.Instance).As<ITargetPlugin>().SingleInstance();
                    foreach (var i in renewal.InstallationPluginOptions)
                    {
                        builder.RegisterInstance(i).As(i.GetType());
                    }
                    foreach (var i in renewal.StorePluginOptions)
                    {
                        builder.RegisterInstance(i).As(i.GetType());
                    }
                }
            });
        }

        /// <summary>
        /// Validation
        /// </summary>
        /// <param name="execution"></param>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public ILifetimeScope Validation(ILifetimeScope execution, ValidationPluginOptions options)
        {
            return execution.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<HttpValidationParameters>();
                builder.RegisterType(options.Instance).
                    As<IValidationPlugin>().
                    SingleInstance();
            });
        }
    }
}
