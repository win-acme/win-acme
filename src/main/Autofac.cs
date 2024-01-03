using Autofac;
using Autofac.Core;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.ValidationPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Interfaces;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Host
{
    internal static class Autofac
    {
        /// <summary>
        /// Configure dependency injection container
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        internal static ILifetimeScope Container(string[] args, bool verbose)
        {
            var builder = new ContainerBuilder();
            _ = builder.RegisterType<LogService>().WithParameter(new TypedParameter(typeof(bool), verbose)).SingleInstance().As<ILogService>();
            _ = builder.RegisterType<AssemblyService>().SingleInstance();
            _ = builder.RegisterType<PluginService>().SingleInstance().As<IPluginService>();
            _ = builder.RegisterType<ArgumentsParser>().WithParameter(new TypedParameter(typeof(string[]), args)).SingleInstance();
            _ = builder.RegisterType<SettingsService>().As<ISettingsService>().SingleInstance();
            var plugin = builder.Build();

            var pluginService = plugin.Resolve<IPluginService>();
            return plugin.BeginLifetimeScope("wacs", builder =>
            {
                // Plugins
                foreach (var plugin in pluginService.GetPlugins()) {
                    _ = builder.RegisterType(plugin.OptionsJson);
                }                
                foreach (var plugin in pluginService.GetSecretServices()) {
                    _ = builder.RegisterType(plugin.Backend);
                }
                foreach (var plugin in pluginService.GetNotificationTargets()) {
                    _ = builder.RegisterType(plugin.Backend);
                }
                WacsJson.Configure(builder);

                // Single instance types
                _ = builder.RegisterType<AdminService>().SingleInstance();
                _ = builder.RegisterType<VersionService>().SingleInstance();
                _ = builder.RegisterType<UserRoleService>().As<IUserRoleService>().SingleInstance();
                _ = builder.RegisterType<ValidationOptionsService>().As<IValidationOptionsService>().As<ValidationOptionsService>().SingleInstance();
                _ = builder.RegisterType<InputService>().As<IInputService>().SingleInstance();
                _ = builder.RegisterType<ProxyService>().As<IProxyService>().SingleInstance();
                _ = builder.RegisterType<UpdateClient>().SingleInstance();
                _ = builder.RegisterType<RenewalStoreDisk>().As<IRenewalStoreBackend>().SingleInstance();
                _ = builder.RegisterType<RenewalStore>().As<IRenewalStore>().SingleInstance();
                _ = builder.RegisterType<DomainParseService>().SingleInstance();
                _ = builder.RegisterType<IISClient>().As<IIISClient>().InstancePerLifetimeScope();
                _ = builder.RegisterType<IISHelper>().SingleInstance();
                _ = builder.RegisterType<ExceptionHandler>().SingleInstance();
                _ = builder.RegisterType<AutofacBuilder>().As<IAutofacBuilder>().SingleInstance();
                _ = builder.RegisterType<AccountManager>().SingleInstance();
                _ = builder.RegisterType<AcmeClientManager>().SingleInstance();
                _ = builder.RegisterType<NetworkCheckService>().SingleInstance();
                _ = builder.RegisterType<ZeroSsl>().SingleInstance();
                _ = builder.RegisterType<OrderManager>().SingleInstance();
                _ = builder.RegisterType<PemService>().SingleInstance();
                _ = builder.RegisterType<EmailClient>().SingleInstance();
                _ = builder.RegisterType<ScriptClient>().SingleInstance();
                _ = builder.RegisterType<LookupClientProvider>().SingleInstance();
                _ = builder.RegisterType<CacheService>().As<ICacheService>().SingleInstance();
                _ = builder.RegisterType<CertificatePicker>().SingleInstance();
                _ = builder.RegisterType<DueDateStaticService>().SingleInstance();
                _ = builder.RegisterType<DueDateRuntimeService>().SingleInstance();
                _ = builder.RegisterType<SecretServiceManager>().SingleInstance();
                _ = builder.RegisterType<TaskSchedulerService>().SingleInstance();
                _ = builder.RegisterType<NotificationService>().SingleInstance();
                _ = builder.RegisterType<RenewalExecutor>().SingleInstance();
                _ = builder.RegisterType<RenewalManager>().SingleInstance();
                _ = builder.RegisterType<RenewalCreator>().SingleInstance();
                _ = builder.RegisterType<RenewalDescriber>().SingleInstance();
                _ = builder.RegisterType<RenewalRevoker>().As<IRenewalRevoker>().SingleInstance();
                _ = builder.RegisterType<Unattended>().SingleInstance();
                _ = builder.RegisterType<ArgumentsInputService>().SingleInstance();
                _ = builder.RegisterType<MainMenu>().SingleInstance();

                // Multi-instance types
                _ = builder.RegisterType<Wacs>();
                _ = builder.RegisterType<UnattendedResolver>();
                _ = builder.RegisterType<InteractiveResolver>();

                // Specials
                _ = builder.RegisterType<HttpValidationParameters>().InstancePerLifetimeScope();
                _ = builder.Register(c => c.Resolve<ArgumentsParser>().GetArguments<MainArguments>()!);
                _ = builder.Register(c => c.Resolve<ArgumentsParser>().GetArguments<AccountArguments>()!);
                _ = builder.Register(c => (ISharingLifetimeScope)c.Resolve<ILifetimeScope>()).As<ISharingLifetimeScope>().ExternallyOwned();
            });
        }
    }
}