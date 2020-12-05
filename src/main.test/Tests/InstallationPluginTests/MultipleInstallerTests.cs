using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.Resolvers;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using mock = PKISharp.WACS.UnitTests.Mock.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.UnitTests.Mock;

namespace PKISharp.WACS.UnitTests.Tests.InstallationPluginTests
{
    [TestClass]
    public class MultipleInstallerTests
    {
        private readonly ILogService log;
        private readonly mock.MockPluginService plugins;
        private readonly mock.MockSettingsService settings;
        private readonly VersionService version;

        public MultipleInstallerTests()
        {
            log = new mock.LogService(false);
            plugins = new mock.MockPluginService(log);
            settings = new mock.MockSettingsService();
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
            var chosen = new List<IInstallationPluginOptionsFactory>();
            
            
            var builder = new ContainerBuilder();
            _ = builder.RegisterType<LookupClientProvider>();
            _ = builder.RegisterType<ProxyService>();
            _ = builder.RegisterType<DomainParseService>();
            _ = builder.RegisterType<IISHelper>();
            _ = builder.RegisterType<VersionService>();
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
            _ = builder.RegisterType<ArgumentsService>().
                As<IArgumentsService>().
                SingleInstance();
            _ = builder.RegisterType<mock.UserRoleService>().As<IUserRoleService>().SingleInstance();
            _ = builder.RegisterType<UnattendedResolver>().As<IResolver>();
            plugins.Configure(builder);

            var scope = builder.Build();
            var resolver = scope.Resolve<IResolver>();
            var first = await resolver.GetInstallationPlugin(scope, types, chosen);
            Assert.IsNotNull(first);
            if (first != null)
            {
                Assert.IsInstanceOfType(first, typeof(IISWebOptionsFactory));
                chosen.Add(first);
                var second = await resolver.GetInstallationPlugin(scope, types, chosen);
                Assert.IsNotNull(second);
                Assert.IsInstanceOfType(second, typeof(NullInstallationOptionsFactory));
            }
        }
    }
}