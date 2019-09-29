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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.UnitTests.Tests.InstallationPluginTests
{
    [TestClass]
    public class MultipleInstallerTests
    {
        private readonly ILogService log;
        private readonly PluginService plugins;

        public MultipleInstallerTests()
        {
            plugins = new PluginService(log);
            log = new Mock.Services.LogService(false);
        }

        [TestMethod]
        public async Task Regular()
        {
            var commandLine = "--installation iis";
            var types = new List<Type>() { typeof(CertificateStore) };
            var chosen = new List<IInstallationPluginOptionsFactory>();


            var builder = new ContainerBuilder();
            builder.RegisterInstance(plugins).
              As<PluginService>().
              SingleInstance();
            builder.RegisterInstance(log).
                As<ILogService>().
                SingleInstance();
            builder.RegisterType<Mock.Clients.MockIISClient>().
                As<IIISClient>().
                SingleInstance();
            builder.RegisterType<ArgumentsParser>().
                SingleInstance().
                WithParameter(new TypedParameter(typeof(string[]), commandLine.Split(' ')));
            builder.RegisterType<ArgumentsService>().
                As<IArgumentsService>().
                SingleInstance();
            builder.RegisterType<UserRoleService>().SingleInstance();
            builder.RegisterType<UnattendedResolver>().As<IResolver>();
            plugins.Configure(builder);

            var scope = builder.Build();
            var resolver = scope.Resolve<IResolver>();
            var first = await resolver.GetInstallationPlugin(scope, types, chosen);
            Assert.IsNotNull(first);
            Assert.IsInstanceOfType(first, typeof(IISWebOptionsFactory));
            chosen.Add(first);
            var second = await resolver.GetInstallationPlugin(scope, types, chosen);
            Assert.IsNotNull(second);
            Assert.IsInstanceOfType(second, typeof(NullInstallationOptionsFactory));
        }
    }
}