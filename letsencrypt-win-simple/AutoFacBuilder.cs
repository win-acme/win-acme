using Autofac;
using Microsoft.Win32;
using Nager.PublicSuffix;
using PKISharp.WACS.Acme;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
using System.Collections.Generic;

namespace PKISharp.WACS
{
    internal class AutofacBuilder
    {
        internal static IContainer Global(string[] args)
        {
            var builder = new ContainerBuilder();

            var logger = new LogService();
            builder.RegisterInstance(logger);

            builder.RegisterType<LogService>().
                As<ILogService>().
                SingleInstance();

            builder.Register(c => new OptionsParser(logger, args).Options).
                As<Options>().
                SingleInstance();

            builder.RegisterType<OptionsService>().
                As<IOptionsService>().
                SingleInstance();

            builder.RegisterType<SettingsService>().
                As<ISettingsService>().
                SingleInstance();

            builder.RegisterType<InputService>().
                As<IInputService>().
                SingleInstance();

            builder.RegisterType<ProxyService>().
                SingleInstance();

            builder.RegisterType<RenewalService>().
               As<IRenewalService>().
               SingleInstance();

            builder.RegisterType<DotNetVersionService>().
                SingleInstance();

            var pluginService = new PluginService(logger);
            pluginService.Configure(builder);

            builder.Register(c => new DomainParser(new WebTldRuleProvider())).SingleInstance();
            builder.RegisterType<IISClient>().SingleInstance();
            builder.RegisterType<UnattendedResolver>();
            builder.RegisterType<InteractiveResolver>();
            builder.RegisterInstance(pluginService);

            return builder.Build();
        }

        internal static ILifetimeScope Legacy(ILifetimeScope main, string baseUri)
        {
            return main.BeginLifetimeScope(builder =>
            {
                builder.Register(c => new Options { BaseUri = baseUri }).
                    As<Options>().
                    SingleInstance();

                builder.RegisterType<Importer>().
                    SingleInstance();

                builder.RegisterType<OptionsService>().
                    As<IOptionsService>().
                    SingleInstance();

                builder.RegisterType<LegacySettingsService>().
                    As<ISettingsService>().
                    SingleInstance();

                // Check where to load Renewals from
                var hive = Registry.CurrentUser;
                var key = hive.OpenSubKey($"Software\\letsencrypt-win-simple");
                if (key == null)
                {
                    hive = Registry.LocalMachine;
                    key = hive.OpenSubKey($"Software\\letsencrypt-win-simple");
                }
                if (key != null)
                {
                    builder.RegisterType<RegistryLegacyRenewalService>().
                            As<ILegacyRenewalService>().
                            WithParameter(new NamedParameter("hive", hive.Name)).
                            SingleInstance();
                }
                else
                {
                    builder.RegisterType<FileLegacyRenewalService>().
                        As<ILegacyRenewalService>().
                        SingleInstance();
                }
            });
        }


        internal static ILifetimeScope Configuration(ILifetimeScope main, ScheduledRenewal renewal, RunLevel runLevel)
        {
            IResolver resolver = null;
            if (runLevel > RunLevel.Unattended)
            {
                resolver = main.Resolve<InteractiveResolver>(
                    new TypedParameter(typeof(ScheduledRenewal), renewal), 
                    new TypedParameter(typeof(RunLevel), runLevel));
            }
            else
            {
                resolver = main.Resolve<UnattendedResolver>(new TypedParameter(typeof(ScheduledRenewal), renewal));
            }
            return main.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<AcmeClient>().SingleInstance();
                builder.RegisterType<CertificateService>().SingleInstance();

                builder.RegisterInstance(resolver);
                if (renewal != null)
                {
                    builder.RegisterInstance(renewal);
                }
                builder.Register(c => runLevel).As<RunLevel>();

                builder.RegisterType<TaskSchedulerService>().
                    WithParameter(new TypedParameter(typeof(RunLevel), runLevel)).
                    SingleInstance();

                builder.Register(c => resolver.GetTargetPlugin(main)).As<ITargetPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetInstallationPlugins(main)).As<List<IInstallationPluginFactory>>().SingleInstance();
                builder.Register(c => resolver.GetValidationPlugin(main)).As<IValidationPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetStorePlugin(main)).As<IStorePluginFactory>().SingleInstance();

                builder.Register(c => c.Resolve(c.Resolve<ITargetPluginFactory>().Instance)).As<ITargetPlugin>().SingleInstance();
            });
        }

        internal static ILifetimeScope Execution(ILifetimeScope main, ScheduledRenewal renewal, RunLevel runLevel)
        {
            return main.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<AcmeClient>().SingleInstance();
                builder.RegisterType<CertificateService>().SingleInstance();
                if (renewal != null)
                {
                    builder.RegisterInstance(renewal);
                }
                builder.Register(c => runLevel).As<RunLevel>();
                builder.RegisterType<TaskSchedulerService>().
                    WithParameter(new TypedParameter(typeof(RunLevel), runLevel)).
                    SingleInstance();

                builder.RegisterInstance(renewal.StorePluginOptions).As(renewal.StorePluginOptions.GetType());
                builder.RegisterInstance(renewal.ValidationPluginOptions).As(renewal.ValidationPluginOptions.GetType());
                builder.RegisterType(renewal.StorePluginOptions.Instance).As<IStorePlugin>().SingleInstance();
                builder.RegisterType(renewal.ValidationPluginOptions.Instance).As<IValidationPlugin>().SingleInstance();
            });
        }

        internal static ILifetimeScope Validation(ILifetimeScope renewalScope, ValidationPluginOptions options, Target target, string identifier)
        {
            return renewalScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterType(options.Instance).
                    WithParameters(new[] {
                        new TypedParameter(typeof(string), identifier),
                        new TypedParameter(typeof(Target), target)
                    }).
                    As<IValidationPlugin>().
                    SingleInstance();
            });
        }
    }
}
