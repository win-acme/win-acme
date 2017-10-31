using Autofac;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple
{
    class AutofacBuilder
    {
        internal static IContainer Regular(string[] args, string _clientName)
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

            builder.RegisterType<LetsEncryptClient>().
                SingleInstance();

            builder.RegisterType<PluginService>().
                SingleInstance();

            return builder.Build();
        }

        internal static ILifetimeScope Renewal(IContainer main, ScheduledRenewal renewal)
        {
            var scope = main.BeginLifetimeScope();
            //builder =>
            //{
            //    builder.reg(() => { return "string"; })
            //});
            return scope;
        }
    }
}
