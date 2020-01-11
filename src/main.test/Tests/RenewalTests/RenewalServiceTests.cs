using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Configuration;
using real = PKISharp.WACS.Services;
using mock = PKISharp.WACS.UnitTests.Mock.Services;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.DNS;

namespace PKISharp.WACS.UnitTests.Tests.RenewalTests
{
    [TestClass]
    public class RenewalManagerTests
    {
        [TestMethod]
        public void Simple()
        {
            var log = new mock.LogService(false);
            var pluginService = new real.PluginService(log);
            var argumentsParser = new ArgumentsParser(log, pluginService, $"".Split(' '));
            var argumentsService = new real.ArgumentsService(log, argumentsParser);

            var builder = new ContainerBuilder();
            _ = builder.RegisterInstance(log).As<real.ILogService>();
            _ = builder.RegisterInstance(argumentsParser).As<ArgumentsParser>();
            _ = builder.RegisterInstance(argumentsService).As<real.IArgumentsService>();
            _ = builder.RegisterType< mock.MockRenewalStore>().As<real.IRenewalStore>();
            _ = builder.RegisterType<mock.MockSettingsService>().As<real.ISettingsService>();
            _ = builder.RegisterInstance(argumentsService).As<real.IArgumentsService>();
            _ = builder.RegisterInstance(pluginService).As<real.IPluginService>();
            _ = builder.RegisterType<real.UserRoleService>().SingleInstance();
            _ = builder.RegisterType<real.InputService>().As<real.IInputService>().SingleInstance();
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
            _ = builder.RegisterType<real.PemService>().SingleInstance();
            _ = builder.RegisterType<EmailClient>().SingleInstance();
            _ = builder.RegisterType<ScriptClient>().SingleInstance();
            _ = builder.RegisterType<LookupClientProvider>().SingleInstance();
            _ = builder.RegisterType<mock.CertificateService>().As<real.ICertificateService>().SingleInstance();
            _ = builder.RegisterType<real.TaskSchedulerService>().SingleInstance();
            _ = builder.RegisterType<real.NotificationService>().SingleInstance();
            _ = builder.RegisterType<RenewalExecutor>().SingleInstance();
            _ = builder.RegisterType<RenewalManager>().SingleInstance();
            _ = builder.Register(c => c.Resolve<real.IArgumentsService>().MainArguments).SingleInstance();

            var container = builder.Build();
            var renewalExecutor = container.Resolve<RenewalExecutor>(
              new TypedParameter(typeof(IContainer), container));
            var renewalManager = container.Resolve<RenewalManager>(
                new TypedParameter(typeof(IContainer), container),
                new TypedParameter(typeof(RenewalExecutor), renewalExecutor));
            Assert.IsNotNull(renewalManager);
        }

    }
}
