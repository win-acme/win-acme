using Autofac;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Plugins.Resolvers;
using LetsEncrypt.ACME.Simple.Services;
using LetsEncrypt.ACME.Simple.Services.Renewal;
using Microsoft.Win32;
using Nager.PublicSuffix;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple
{
    class AutofacBuilder
    {
        internal static IContainer Global(string[] args, string clientName, PluginService pluginService)
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<LogService>().
                As<ILogService>().
                SingleInstance();

            builder.RegisterType<OptionsService>().
                As<IOptionsService>().
                WithParameter(new TypedParameter(typeof(string[]), args)).
                SingleInstance();

            builder.RegisterType<SettingsService>().
                WithParameter(new TypedParameter(typeof(string), clientName)).
                SingleInstance();

            builder.RegisterType<InputService>().
                As<IInputService>().
                SingleInstance();

            builder.RegisterType<ProxyService>().
                SingleInstance();

            // Check where to load Renewals from
            var hive = Registry.CurrentUser;
            var key = hive.OpenSubKey($"Software\\{clientName}");
            if (key == null)
            {
                hive = Registry.LocalMachine;
                key = hive.OpenSubKey($"Software\\{clientName}");
            }
            if (key != null)
            {
                builder.RegisterType<RegistryRenewalService>().
                        As<IRenewalService>().
                        WithParameter(new NamedParameter("hive", hive.Name)).
                        SingleInstance();
            }
            else
            {
                builder.RegisterType<FileRenewalService>().
                    As<IRenewalService>().
                    SingleInstance();
            }

            builder.RegisterType<DotNetVersionService>().
                SingleInstance();

            pluginService.Configure(builder);

            builder.RegisterInstance(clientName).Named<string>("clientName");

            builder.Register(c => new DomainParser(new WebTldRuleProvider())).SingleInstance();
            builder.RegisterType<IISClient>().SingleInstance();
            builder.RegisterType<UnattendedResolver>();
            builder.RegisterType<InteractiveResolver>();
            builder.RegisterInstance(pluginService);

            return builder.Build();
        }

        internal static ILifetimeScope Renewal(IContainer main, ScheduledRenewal renewal, RunLevel runLevel)
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
                builder.RegisterInstance(renewal);

                builder.Register(c => runLevel).As<RunLevel>();

                builder.RegisterType<TaskSchedulerService>().
                    WithParameter(new TypedParameter(typeof(RunLevel), runLevel)).
                    WithParameter(new TypedParameter(typeof(string), main.ResolveNamed<string>("clientName"))).
                    SingleInstance();

                builder.Register(c => resolver.GetTargetPlugin(main)).As<ITargetPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetInstallationPlugins(main)).As<List<IInstallationPluginFactory>>().SingleInstance();
                builder.Register(c => resolver.GetValidationPlugin(main)).As<IValidationPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetStorePlugin(main)).As<IStorePluginFactory>().SingleInstance();

                builder.Register(c => c.Resolve(c.Resolve<ITargetPluginFactory>().Instance)).As<ITargetPlugin>().SingleInstance();
                builder.Register(c => c.Resolve(c.Resolve<IStorePluginFactory>().Instance)).As<IStorePlugin>().SingleInstance();
            });
        }

        internal static ILifetimeScope Identifier(ILifetimeScope renewalScope, Target target, string identifier)
        {
            return renewalScope.BeginLifetimeScope(builder =>
            {
                builder.Register(c => c.Resolve(
                    c.Resolve<IValidationPluginFactory>().Instance, 
                    new TypedParameter(typeof(string), identifier), 
                    new TypedParameter(typeof(Target), target))).As<IValidationPlugin>().SingleInstance();
            });
        }
    }
}
