using Autofac;
using Autofac.Core;
using Autofac.Features.AttributeFilters;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Services.Serialization;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Collections.Generic;
using Real = PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Mock
{
    class MockContainer
    {
        public ILifetimeScope TestScope(List<string>? inputSequence = null, string commandLine = "")
        {
            var log = new Services.LogService(false);
            var assemblyService = new MockAssemblyService(log);
            var pluginService = new Real.PluginService(log, assemblyService);
            var argumentsParser = new ArgumentsParser(log, assemblyService, commandLine.Split(' '));
            var input = new Services.InputService(inputSequence ?? new List<string>());

            var builder = new ContainerBuilder();
            _ = builder.RegisterType<Services.SecretService>().As<Real.ISecretService>().SingleInstance();
            _ = builder.RegisterType<Real.SecretServiceManager>();
            _ = builder.RegisterType<AccountManager>();
            _ = builder.RegisterType<ZeroSsl>();
            WacsJson.Configure(builder);
            _ = builder.RegisterInstance(log).As<Real.ILogService>();
            _ = builder.RegisterInstance(argumentsParser).As<ArgumentsParser>();
            _ = builder.RegisterType<Real.ArgumentsInputService>();
            _ = builder.RegisterInstance(pluginService).As<Real.IPluginService>();
            _ = builder.RegisterInstance(input).As<Real.IInputService>();
            _ = builder.RegisterInstance(argumentsParser.GetArguments<MainArguments>()!).SingleInstance();
            _ = builder.RegisterType<Real.ValidationOptionsService>().As<Real.IValidationOptionsService>().SingleInstance().WithAttributeFiltering(); ;
            _ = builder.RegisterType<Real.RenewalStore>().As<Real.IRenewalStore>().SingleInstance();
            _ = builder.RegisterType<Services.MockRenewalStore>().As<Real.IRenewalStoreBackend>().SingleInstance();
            _ = builder.RegisterType<Real.DueDateStaticService>().As<Real.IDueDateService>().SingleInstance();
            _ = builder.RegisterType<Services.MockSettingsService>().As<Real.ISettingsService>().SingleInstance(); ;
            _ = builder.RegisterType<Services.UserRoleService>().As<Real.IUserRoleService>().SingleInstance();
            _ = builder.RegisterType<Services.ProxyService>().As<Real.IProxyService>().SingleInstance();
            _ = builder.RegisterType<Real.PasswordGenerator>().SingleInstance();
            _ = builder.RegisterType<RenewalCreator>().SingleInstance();
            _ = builder.RegisterType<Real.SecretServiceManager>();
            _ = builder.RegisterType<Real.DomainParseService>().SingleInstance();
            _ = builder.RegisterType<Mock.Clients.MockIISClient>().As<IIISClient>().SingleInstance();
            _ = builder.RegisterType<IISHelper>().SingleInstance();
            _ = builder.RegisterType<Real.ExceptionHandler>().SingleInstance();
            _ = builder.RegisterType<UnattendedResolver>();
            _ = builder.RegisterType<InteractiveResolver>();
            _ = builder.RegisterType<Real.AutofacBuilder>().As<Real.IAutofacBuilder>().SingleInstance();
            _ = builder.RegisterType<AcmeClient>().SingleInstance();
            _ = builder.RegisterType<ZeroSsl>().SingleInstance();
            _ = builder.RegisterType<Real.PemService>().SingleInstance();
            _ = builder.RegisterType<EmailClient>().SingleInstance();
            _ = builder.RegisterType<ScriptClient>().SingleInstance();
            _ = builder.RegisterType<LookupClientProvider>().SingleInstance();
            _ = builder.RegisterType<CacheService>().As<Real.ICacheService>().SingleInstance();
            _ = builder.RegisterType<CertificateService>().As<Real.ICertificateService>().SingleInstance();
            _ = builder.RegisterType<Real.TaskSchedulerService>().SingleInstance();
            _ = builder.RegisterType<Real.NotificationService>().SingleInstance();
            _ = builder.RegisterType<RenewalValidator>().SingleInstance();
            _ = builder.RegisterType<RenewalExecutor>().SingleInstance();
            _ = builder.RegisterType<OrderProcessor>().SingleInstance();
            _ = builder.RegisterType<RenewalManager>().SingleInstance();
            _ = builder.Register(c => (ISharingLifetimeScope)c.Resolve<ILifetimeScope>()).As<ISharingLifetimeScope>().ExternallyOwned();

            return builder.Build();
        }
    }
}
