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
        internal static IContainer Global(string[] args, string _clientName)
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
                WithParameter(new TypedParameter(typeof(string), _clientName)).
                SingleInstance();

            builder.RegisterType<InputService>().
                As<IInputService>().
                SingleInstance();

            builder.RegisterType<ProxyService>().
                SingleInstance();

            builder.RegisterType<PluginService>().
                SingleInstance();

            builder.RegisterType<LetsEncryptClient>();
            builder.RegisterType<Resolver>();

            return builder.Build();
        }

        internal static ILifetimeScope Renewal(IContainer main, PluginService pluginService, ScheduledRenewal renewal)
        {
            var resolver = main.Resolve<Resolver>(new TypedParameter(typeof(ScheduledRenewal), renewal));
            var scope = main.BeginLifetimeScope(builder =>
            {
                pluginService.Target.ForEach(t => { builder.RegisterType(t.GetType()); });
                pluginService.Validation.ForEach(t => { builder.RegisterType(t.GetType()); });
                pluginService.Store.ForEach(t => { builder.RegisterType(t); });
                pluginService.Installation.ForEach(t => { builder.RegisterType(t.GetType()); });
                pluginService.InstallationInstance.ForEach(ip => { builder.RegisterType(ip); });

                builder.RegisterInstance(resolver);
                builder.RegisterInstance(renewal);

                builder.Register(c => { return resolver.GetTargetPlugin(); });
                builder.Register(c => { return resolver.GetInstallationPlugin(); });
                builder.Register(c => { return resolver.GetValidationPlugin(); });
                builder.Register(c => { return resolver.GetStorePlugin(); });
            });
            resolver.Scope = scope;
            return scope;
        }
    }
}
