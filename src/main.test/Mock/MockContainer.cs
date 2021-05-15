using Autofac;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Plugins.Resolvers;
using System.Collections.Generic;
using mock = PKISharp.WACS.UnitTests.Mock.Services;
using real = PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Mock
{
    class MockContainer
    {
        public ILifetimeScope TestScope(List<string>? inputSequence = null, string commandLine = "")
        {
            var log = new mock.LogService(false);
            var pluginService = new real.PluginService(log);
            var argumentsParser = new ArgumentsParser(log, pluginService, commandLine.Split(' '));
            var input = new mock.InputService(inputSequence ?? new List<string>());

            var builder = new ContainerBuilder();
            _ = builder.RegisterType<mock.SecretService>().As<real.ISecretService>().SingleInstance();
            _ = builder.RegisterType<real.SecretServiceManager>();
            _ = builder.RegisterInstance(log).As<real.ILogService>();
            _ = builder.RegisterInstance(argumentsParser).As<ArgumentsParser>();
            _ = builder.RegisterType<real.ArgumentsInputService>();
            _ = builder.RegisterInstance(pluginService).As<real.IPluginService>();
            _ = builder.RegisterInstance(input).As<real.IInputService>();
            _ = builder.RegisterInstance(argumentsParser.GetArguments<MainArguments>()!).SingleInstance();

            _ = builder.RegisterType<mock.MockRenewalStore>().As<real.IRenewalStore>().SingleInstance();
            _ = builder.RegisterType<mock.MockSettingsService>().As<real.ISettingsService>().SingleInstance(); ;
            _ = builder.RegisterType<mock.UserRoleService>().As<real.IUserRoleService>().SingleInstance();
            _ = builder.RegisterType<mock.ProxyService>().As<real.IProxyService>().SingleInstance();
            _ = builder.RegisterType<real.PasswordGenerator>().SingleInstance();
            _ = builder.RegisterType<real.SecretServiceManager>();

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

            return builder.Build();
        }
    }
}
