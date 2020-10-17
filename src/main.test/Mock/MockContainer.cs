using Autofac;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Resolvers;
using System.Collections.Generic;
using mock = PKISharp.WACS.UnitTests.Mock.Services;
using real = PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Mock
{
    class MockContainer
    {
        public ILifetimeScope TestScope()
        {
            var log = new mock.LogService(false);
            var versionService = new real.VersionService(log);
            var pluginService = new real.PluginService(log, versionService);
            var argumentsParser = new ArgumentsParser(log, pluginService, $"".Split(' '));
            var argumentsService = new real.ArgumentsService(log, argumentsParser);
            var input = new mock.InputService(new List<string>()
            {
                "C", // Cancel command
                "y", // Confirm cancel all
                "Q" // Quit
            });

            var builder = new ContainerBuilder();
            _ = builder.RegisterInstance(log).As<real.ILogService>();
            _ = builder.RegisterInstance(argumentsParser).As<ArgumentsParser>();
            _ = builder.RegisterInstance(argumentsService).As<real.IArgumentsService>();
            _ = builder.RegisterInstance(argumentsService).As<real.IArgumentsService>();
            _ = builder.RegisterInstance(pluginService).As<real.IPluginService>();
            _ = builder.RegisterInstance(input).As<real.IInputService>();

            _ = builder.RegisterType<mock.MockRenewalStore>().As<real.IRenewalStore>().SingleInstance();
            _ = builder.RegisterType<mock.MockSettingsService>().As<real.ISettingsService>().SingleInstance(); ;
            _ = builder.RegisterType<mock.UserRoleService>().As<real.IUserRoleService>().SingleInstance();
            _ = builder.RegisterType<real.VersionService>().SingleInstance();
            _ = builder.RegisterType<real.ProxyService>().SingleInstance();
            _ = builder.RegisterType<real.PasswordGenerator>().SingleInstance();

            pluginService.Configure(builder);

            _ = builder.RegisterType<real.DomainParseService>().SingleInstance();
            _ = builder.RegisterType<Mock.Clients.MockIISClient>().As<IIISClient>().SingleInstance();
            _ = builder.RegisterType<IISHelper>().SingleInstance();
            _ = builder.RegisterType<real.ExceptionHandler>().SingleInstance();
            _ = builder.RegisterType<UnattendedResolver>();
            _ = builder.RegisterType<InteractiveResolver>();
            _ = builder.RegisterType<real.AutofacBuilder>().As<real.IAutofacBuilder>().SingleInstance();
            _ = builder.RegisterType<AcmeClient>().SingleInstance();
            _ = builder.RegisterType<ZeroSsl>().SingleInstance();
            _ = builder.RegisterType<real.PemService>().SingleInstance();
            _ = builder.RegisterType<EmailClient>().SingleInstance();
            _ = builder.RegisterType<ScriptClient>().SingleInstance();
            _ = builder.RegisterType<LookupClientProvider>().SingleInstance();
            _ = builder.RegisterType<mock.CertificateService>().As<real.ICertificateService>().SingleInstance();
            _ = builder.RegisterType<real.TaskSchedulerService>().SingleInstance();
            _ = builder.RegisterType<real.NotificationService>().SingleInstance();
            _ = builder.RegisterType<RenewalValidator>().SingleInstance();
            _ = builder.RegisterType<RenewalExecutor>().SingleInstance();
            _ = builder.RegisterType<RenewalManager>().SingleInstance();
            _ = builder.Register(c => c.Resolve<real.IArgumentsService>().MainArguments).SingleInstance();

            return builder.Build();
        }
    }
}
