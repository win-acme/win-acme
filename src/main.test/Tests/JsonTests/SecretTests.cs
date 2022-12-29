using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using PKISharp.WACS.UnitTests.Mock.Services;
using System;
using System.Text.Json;

namespace PKISharp.WACS.UnitTests.Tests.JsonTests
{
    [TestClass]
    public class SecretTests
    {
        private readonly ILifetimeScope _container;

        public SecretTests()
        {
            var builder = new ContainerBuilder();
            _ = builder.RegisterType<MockSettingsService>().As<ISettingsService>();
            _ = builder.RegisterType<MockAssemblyService>().As<AssemblyService>();
            _ = builder.RegisterType<Mock.Services.LogService>().As<ILogService>();
            _ = builder.RegisterType<PluginService>().As<IPluginService>();
            WacsJson.Configure(builder);
            _container = builder.Build();
        }

        private string Serialize(Renewal renewal)
        {
            var wacsJson = _container.ResolveNamed<WacsJson>("current");
            return JsonSerializer.Serialize(renewal, wacsJson.Renewal);
        }

        private Renewal Deserialize(string json)
        {
            var wacsJson = _container.ResolveNamed<WacsJson>("legacy");
            var renewal = JsonSerializer.Deserialize(json, wacsJson.Renewal);
            Assert.IsNotNull(renewal);
            return renewal;
        }

        [TestMethod]
        public void SerializeSecretCorrect()
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
            Assert.IsFalse(serialize.Contains("null"));
            Assert.IsNotNull(newRenewal);
            Assert.IsInstanceOfType(newRenewal.ValidationPluginOptions, typeof(FtpOptions));
            var ftpOptions = newRenewal.ValidationPluginOptions as FtpOptions;
            Assert.IsNotNull(ftpOptions);
            Assert.IsNotNull(ftpOptions.Credential);
            Assert.IsNotNull(ftpOptions.Credential.Password);
            Assert.AreEqual(ftpOptions.Credential.Password.Value, "password");
        }
    }
}
