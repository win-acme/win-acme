using Autofac;
using Autofac.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Linq;
using System.Text.Json;
using ManualOptions = PKISharp.WACS.Plugins.TargetPlugins.ManualOptions;

namespace PKISharp.WACS.UnitTests.Tests.JsonTests
{
    [TestClass]
    public class SecretTests
    {
        private static ILifetimeScope? _container;
        private static IPluginService? _plugin;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var builder = new ContainerBuilder();
            var log = new Mock.Services.LogService();
            var assembly = new AssemblyService(log);
            var plugin = new PluginService(log, assembly);
            _ = builder.RegisterType<MockSettingsService>().As<ISettingsService>();
            _ = builder.RegisterInstance(assembly).As<AssemblyService>().SingleInstance();
            _ = builder.RegisterInstance(log).As<ILogService>();
            _ = builder.RegisterInstance(plugin).As<IPluginService>().SingleInstance();
            _ = builder.Register(c => (ISharingLifetimeScope)c.Resolve<ILifetimeScope>()).As<ISharingLifetimeScope>().ExternallyOwned();
            plugin.Configure(builder);
            WacsJson.Configure(builder);
            _container = builder.Build();
            _plugin = _container.Resolve<IPluginService>();
        }

        private static string Serialize(Renewal renewal)
        {
            var wacsJson = _container!.Resolve<WacsJson>();
            return JsonSerializer.Serialize(renewal, wacsJson.Renewal);
        }

        private static Renewal Deserialize(string json)
        {
            var wacsJson = _container!.Resolve<WacsJson>();
            var renewal = JsonSerializer.Deserialize(json, wacsJson.Renewal);
            Assert.IsNotNull(renewal);
            return renewal;
        }

        [TestMethod]
        public void SerializeSecretNative()
        {
            var renewal = new Renewal
            {
                TargetPluginOptions = new ManualOptions(),
                ValidationPluginOptions = new FtpOptions
                {
                    Credential = new NetworkCredentialOptions("user", new ProtectedString("password"))
                }
            };
            var serialize = Serialize(renewal);
            var newRenewal = Deserialize(serialize);
            serialize = Serialize(newRenewal);
            newRenewal = Deserialize(serialize);
            Assert.IsFalse(serialize.Contains("null"));
            Assert.IsNotNull(newRenewal);
            Assert.IsInstanceOfType(newRenewal.ValidationPluginOptions, typeof(FtpOptions));
            var ftpOptions = newRenewal.ValidationPluginOptions as FtpOptions;
            Assert.IsNotNull(ftpOptions);
            Assert.IsNotNull(ftpOptions.Credential);
            Assert.IsNotNull(ftpOptions.Credential.Password);
            Assert.AreEqual(ftpOptions.Credential.Password.Value, "password");
        }

        [TestMethod]
        public void SerializeSecretExternal()
        {
            Assert.AreEqual(31, _plugin!.GetPlugins().Count());
            var renewal = new Renewal
            {
                TargetPluginOptions = new ManualOptions(),
                ValidationPluginOptions = new FtpOptions()
                {
                    Credential = new NetworkCredentialOptions()
                    {
                        Password = new ProtectedString("safe")
                    }
                }
            };
            var serialize = Serialize(renewal);
            var newRenewal = Deserialize(serialize);
            Assert.IsFalse(serialize.Contains("null"));
            Assert.IsNotNull(newRenewal);
            Assert.IsInstanceOfType(newRenewal.ValidationPluginOptions, typeof(FtpOptions));
            var azureOptions = newRenewal.ValidationPluginOptions as FtpOptions;
            Assert.IsNotNull(azureOptions);
            Assert.IsNotNull(azureOptions.Credential);
            Assert.IsNotNull(azureOptions.Credential.Password);
            Assert.AreEqual(azureOptions.Credential.Password.Value, "safe");
        }

    }
}
