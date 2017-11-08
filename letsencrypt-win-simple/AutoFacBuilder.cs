using Autofac;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Plugins;
using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.StorePlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using LetsEncrypt.ACME.Simple.Services;

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

            builder.RegisterType<IISClient>().
                SingleInstance();

            pluginService.Target.ForEach(t => { builder.RegisterInstance(t); });
            pluginService.Validation.ForEach(t => { builder.RegisterInstance(t); });
            pluginService.Store.ForEach(t => { builder.RegisterInstance(t); });
            pluginService.Installation.ForEach(t => { builder.RegisterInstance(t); });

            pluginService.TargetInstance.ForEach(ip => { builder.RegisterType(ip); });
            pluginService.ValidationInstance.ForEach(ip => { builder.RegisterType(ip); });
            pluginService.StoreInstance.ForEach(ip => { builder.RegisterType(ip); });
            pluginService.InstallationInstance.ForEach(ip => { builder.RegisterType(ip); });
        
            builder.RegisterType<LetsEncryptClient>();
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
                builder.RegisterInstance(resolver);
                builder.RegisterInstance(renewal);
                builder.Register(c => resolver.GetTargetPlugin()).As<ITargetPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetInstallationPlugin()).As<IInstallationPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetValidationPlugin()).As<IValidationPluginFactory>().SingleInstance();
                builder.Register(c => resolver.GetStorePlugin()).As<IStorePluginFactory>().SingleInstance();
                builder.Register(c => c.Resolve(c.Resolve<ITargetPluginFactory>().Instance)).As<ITargetPlugin>().SingleInstance();
                builder.Register(c => c.Resolve(c.Resolve<IValidationPluginFactory>().Instance)).As<IValidationPlugin>().SingleInstance();
                builder.Register(c => c.Resolve(c.Resolve<IStorePluginFactory>().Instance)).As<IStorePlugin>().SingleInstance();
                builder.Register(c => c.Resolve(c.Resolve<IInstallationPluginFactory>().Instance)).As<IInstallationPlugin>().SingleInstance();
            });
        }
    }
}
