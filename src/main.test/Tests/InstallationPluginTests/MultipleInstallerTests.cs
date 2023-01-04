using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Plugins;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.UnitTests.Tests.InstallationPluginTests
{
    [TestClass]
    public class MultipleInstallerTests
    {
        private readonly ILogService log;
        private readonly PluginService plugins;
        private readonly MockSettingsService settings;

        public MultipleInstallerTests()
        {
            log = new Mock.Services.LogService(false);
            plugins = new PluginService(log, new MockAssemblyService(log));
            settings = new MockSettingsService();
        }

        /// <summary>
        /// This tests only works when running as admin
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task Regular()
        {
            var commandLine = "--installation iis";
            var types = new List<Type>() { typeof(CertificateStore) };
            var chosen = new List<Plugin>();
            
            
            var builder = new ContainerBuilder();
            _ = builder.RegisterType<LookupClientProvider>();
            _ = builder.RegisterType<Mock.Services.ProxyService>().As<IProxyService>();
            _ = builder.RegisterType<DomainParseService>();
            _ = builder.RegisterType<ArgumentsInputService>();
            _ = builder.RegisterType<IISHelper>();
            _ = builder.RegisterType<MockAssemblyService>().As<AssemblyService>();

            var input = new Mock.Services.InputService(new List<string>());
            _ = builder.RegisterInstance(input).As<IInputService>();
            _ = builder.RegisterType<SecretService>().As<ISecretService>();
            _ = builder.RegisterType<SecretServiceManager>();
            _ = builder.RegisterInstance(plugins).
                As<IPluginService>().
                SingleInstance();
            _ = builder.RegisterInstance(settings).
              As<ISettingsService>().
              SingleInstance();
            _ = builder.RegisterInstance(log).
                As<ILogService>().
                SingleInstance();
            _ = builder.RegisterType<Mock.Clients.MockIISClient>().
                As<IIISClient>().
                SingleInstance();
            _ = builder.RegisterType<ArgumentsParser>().
                SingleInstance().
                WithParameter(new TypedParameter(typeof(string[]), commandLine.Split(' ')));
            _ = builder.RegisterType<Mock.Services.UserRoleService>().As<IUserRoleService>().SingleInstance();
            _ = builder.RegisterType<UnattendedResolver>().As<IResolver>();
            _ = builder.Register(c => c.Resolve<ArgumentsParser>().GetArguments<MainArguments>()!).SingleInstance();
            plugins.Configure(builder);
            
            var scope = builder.Build();
            var resolver = scope.Resolve<IResolver>();
            var first = await resolver.GetInstallationPlugin(
                scope, 
                types.Select(t => plugins.GetPlugins().First(x => x.Backend == t)),
                chosen);
            Assert.IsNotNull(first);
            Assert.AreEqual(first.OptionsFactory, typeof(IISOptionsFactory));
            chosen.Add(first);
            var second = await resolver.GetInstallationPlugin(
                scope,
                types.Select(t => plugins.GetPlugins().First(x => x.Backend == t)),
                chosen);
            Assert.IsNull(second);
        }
    }
}