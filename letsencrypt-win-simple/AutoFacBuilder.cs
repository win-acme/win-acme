using Autofac;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins;
using LetsEncrypt.ACME.Simple.Plugins.Interfaces;
using LetsEncrypt.ACME.Simple.Plugins.Resolvers;
using LetsEncrypt.ACME.Simple.Services;
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
                As<ISettingsService>().
                WithParameter(new TypedParameter(typeof(string), clientName)).
                SingleInstance();

            builder.RegisterType<InputService>().
                As<IInputService>().
                SingleInstance();

            builder.RegisterType<ProxyService>().
                SingleInstance();

            builder.RegisterType<RenewalService>().
                WithParameter(new TypedParameter(typeof(string), clientName)).
                SingleInstance();

            builder.RegisterType<TaskSchedulerService>().
                WithParameter(new TypedParameter(typeof(string), clientName)).
                SingleInstance();

            builder.RegisterType<DotNetVersionService>().
                SingleInstance();

            pluginService.Configure(builder);

            builder.RegisterType<IISClient>().SingleInstance();
            builder.RegisterType<UnattendedResolver>();
            builder.RegisterType<InteractiveResolver>();
            builder.RegisterInstance(pluginService);

            return builder.Build();
        }

        internal static ILifetimeScope Renewal(IContainer main, ScheduledRenewal renewal, bool interactive)
        {
            IResolver resolver = null;
            if (interactive)
            {
                resolver = main.Resolve<InteractiveResolver>(new TypedParameter(typeof(ScheduledRenewal), renewal));
            }
            else
            {
                resolver = main.Resolve<UnattendedResolver>(new TypedParameter(typeof(ScheduledRenewal), renewal));
            }
            return main.BeginLifetimeScope(builder =>
            {
                builder.RegisterType<LetsEncryptClient>().SingleInstance();
                builder.RegisterType<CertificateService>().SingleInstance();

                builder.RegisterInstance(resolver);
                builder.RegisterInstance(renewal);

                builder.Register(c => resolver.GetTargetPlugin(main)).As<ITargetPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetInstallationPlugins(main)).As<List<IInstallationPluginFactory>>().SingleInstance();
                builder.Register(c => resolver.GetValidationPlugin(main)).As<IValidationPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetStorePlugin(main)).As<IStorePluginFactory>().SingleInstance();

                builder.Register(c => c.Resolve(c.Resolve<ITargetPluginFactory>().Instance)).As<ITargetPlugin>().SingleInstance();
                builder.Register(c => c.Resolve(c.Resolve<IValidationPluginFactory>().Instance)).As<IValidationPlugin>();
                builder.Register(c => c.Resolve(c.Resolve<IStorePluginFactory>().Instance)).As<IStorePlugin>().SingleInstance();
            });
        }
    }
}
