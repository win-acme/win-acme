using Autofac;
using Microsoft.Win32;
using PKISharp.WACS.Acme;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.ValidationPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Legacy;
using System.Collections.Generic;

namespace PKISharp.WACS
{
    internal class AutofacBuilder
    {
        internal ILifetimeScope Legacy(ILifetimeScope main, string fromUri, string toUri)
        {
            return main.BeginLifetimeScope(builder =>
            {
                builder.Register(c => new Options { BaseUri = fromUri, ImportBaseUri = toUri }).
                    As<Options>().
                    SingleInstance();

                builder.RegisterType<Importer>().
                    SingleInstance();

                builder.RegisterType<OptionsService>().
                    As<IOptionsService>().
                    SingleInstance();
                
                builder.RegisterType<LegacySettingsService>().
                    As<ISettingsService>().
                    WithParameter(new TypedParameter(typeof(ISettingsService), main.Resolve<ISettingsService>())).
                    SingleInstance();

                builder.RegisterType<LegacyTaskSchedulerService>();
                builder.RegisterType<TaskSchedulerService>().
                    WithParameter(new TypedParameter(typeof(RunLevel), RunLevel.Import)).
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

        internal ILifetimeScope Configuration(ILifetimeScope main, RunLevel runLevel)
        {
            IResolver resolver = null;
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                resolver = main.Resolve<InteractiveResolver>(new TypedParameter(typeof(RunLevel), runLevel));
            }
            else
            {
                resolver = main.Resolve<UnattendedResolver>();
            }
            return main.BeginLifetimeScope(builder =>
            {
                builder.Register(c => runLevel).As<RunLevel>();
                builder.Register(c => resolver.GetTargetPlugin(main)).As<ITargetPluginOptionsFactory>().SingleInstance();
                builder.Register(c => resolver.GetInstallationPlugins(main)).As<List<IInstallationPluginOptionsFactory>>().SingleInstance(); 
                builder.Register(c => resolver.GetStorePlugin(main)).As<IStorePluginOptionsFactory>().SingleInstance();
            });
        }

        internal ILifetimeScope Target(ILifetimeScope main, Renewal renewal, RunLevel runLevel)
        {
            IResolver resolver = null;
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                resolver = main.Resolve<InteractiveResolver>(new TypedParameter(typeof(RunLevel), runLevel));
            }
            else
            {
                resolver = main.Resolve<UnattendedResolver>();
            }
            return main.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(renewal.TargetPluginOptions).As(renewal.TargetPluginOptions.GetType());
                builder.RegisterType(renewal.TargetPluginOptions.Instance).As<ITargetPlugin>().SingleInstance();
                builder.Register(c => c.Resolve<ITargetPlugin>().Generate()).As<Target>().SingleInstance();
                builder.Register(c => resolver.GetValidationPlugin(main, c.Resolve<Target>())).As<IValidationPluginOptionsFactory>().SingleInstance();
            });
        }

        internal ILifetimeScope Execution(ILifetimeScope target, Renewal renewal, RunLevel runLevel)
        {
            return target.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<AcmeClient>().SingleInstance();
                builder.RegisterType<CertificateService>().SingleInstance();

                // Used to configure TaskScheduler without renewal
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
                builder.RegisterInstance(renewal.TargetPluginOptions).As(renewal.TargetPluginOptions.GetType());

                builder.RegisterType(renewal.StorePluginOptions.Instance).As<IStorePlugin>().SingleInstance();
                builder.RegisterType(renewal.ValidationPluginOptions.Instance).As<IValidationPlugin>().SingleInstance();
                builder.RegisterType(renewal.TargetPluginOptions.Instance).As<ITargetPlugin>().SingleInstance();
                foreach (var i in renewal.InstallationPluginOptions)
                {
                    builder.RegisterInstance(i).As(i.GetType());
                }
            });
        }

        internal ILifetimeScope Validation(ILifetimeScope execution, ValidationPluginOptions options, TargetPart target, string identifier)
        {
            return execution.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<HttpValidationParameters>().
                    WithParameters(new[] {
                        new TypedParameter(typeof(string), identifier),
                        new TypedParameter(typeof(TargetPart), target)
                    });
                builder.RegisterType(options.Instance).
                    WithParameters(new[] {
                        new TypedParameter(typeof(string), identifier),
                    }).
                    As<IValidationPlugin>().
                    SingleInstance();
            });
        }
    }
}
