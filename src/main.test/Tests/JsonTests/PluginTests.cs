using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using PKISharp.WACS.UnitTests.Mock.Services;
using System;
using System.Text.Json;

namespace PKISharp.WACS.UnitTests.Tests.JsonTests
{
    [TestClass]
    public class PluginTests
    {
        private readonly ILifetimeScope _container;
        private readonly IPluginService _plugin;
        private readonly ILogService _log;

        public PluginTests()
        {
            var builder = new ContainerBuilder();
            _ = builder.RegisterType<MockSettingsService>().As<ISettingsService>();
            _ = builder.RegisterType<MockAssemblyService>().As<AssemblyService>();
            _ = builder.RegisterType<Mock.Services.LogService>().As<ILogService>();
            _ = builder.RegisterType<PluginService>().As<IPluginService>();
            WacsJson.Configure(builder);
            _container = builder.Build();
            _plugin = _container.Resolve<IPluginService>();
            _log = _container.Resolve<ILogService>();
        }

        private Renewal Deserialize(string json)
        {
            var wacsJson = _container.ResolveNamed<WacsJson>("legacy");
            var renewal = JsonSerializer.Deserialize(json, wacsJson.Renewal);
            Assert.IsNotNull(renewal);
            return renewal;
        }

        [TestMethod]
        public void DeserializeTargetCorrect()
        {
            foreach (var target in _plugin.GetPlugins(Steps.Target))
            {
                var input = @$"{{
                              ""TargetPluginOptions"": {{
                                ""Plugin"": ""{target.Id}""
                              }}
                            }}";
                var renewal = Deserialize(input);
                Assert.IsInstanceOfType(renewal.TargetPluginOptions, target.Meta.Options);
                Assert.AreEqual(renewal.TargetPluginOptions.FindPlugin(_plugin), target);
            }
        }

        [TestMethod]
        public void DeserializeTargetIncorrect()
        {
            var input = @$"{{
                              ""TargetPluginOptions"": {{
                                ""Plugin"": ""incorrect""
                              }}
                            }}";
            var renewal = Deserialize(input);
            Assert.IsNull(renewal.TargetPluginOptions);
        }

        [TestMethod]
        public void DeserializeValidationCorrect()
        {
            foreach (var plugin in _plugin.GetPlugins(Steps.Validation))
            {
                var input = @$"{{
                              ""ValidationPluginOptions"": {{
                                ""Plugin"": ""{plugin.Id}""
                              }}
                            }}";
                var renewal = Deserialize(input);
                _log.Information(plugin.Runner.Name);
                Assert.IsInstanceOfType(renewal.ValidationPluginOptions, plugin.Meta.Options);
                Assert.AreEqual(renewal.ValidationPluginOptions.FindPlugin(_plugin), plugin);
            }
        }

        [TestMethod]
        public void DeserializeCsrCorrect()
        {
            foreach (var plugin in _plugin.GetPlugins(Steps.Csr))
            {
                var input = @$"{{
                              ""CsrPluginOptions"": {{
                                ""Plugin"": ""{plugin.Id}""
                              }}
                            }}";
                var renewal = Deserialize(input);
                _log.Information(plugin.Runner.Name);
                Assert.IsInstanceOfType(renewal.CsrPluginOptions, plugin.Meta.Options);
                Assert.AreEqual(renewal.CsrPluginOptions!.FindPlugin(_plugin), plugin);
            }
        }

        [TestMethod]
        public void DeserializeOrderCorrect()
        {
            foreach (var plugin in _plugin.GetPlugins(Steps.Order))
            {
                var input = @$"{{
                              ""OrderPluginOptions"": {{
                                ""Plugin"": ""{plugin.Id}""
                              }}
                            }}";
                var renewal = Deserialize(input);
                _log.Information(plugin.Runner.Name);
                Assert.IsInstanceOfType(renewal.OrderPluginOptions, plugin.Meta.Options);
                Assert.AreEqual(renewal.OrderPluginOptions!.FindPlugin(_plugin), plugin);
            }
        }

        [TestMethod]
        public void DeserializeStoreCorrectSingle()
        {
            foreach (var plugin in _plugin.GetPlugins(Steps.Store))
            {
                var input = @$"{{
                              ""StorePluginOptions"": {{
                                ""Plugin"": ""{plugin.Id}""
                              }}
                            }}";
                var renewal = Deserialize(input);
                _log.Information(plugin.Runner.Name);
                Assert.IsInstanceOfType(renewal.StorePluginOptions[0], plugin.Meta.Options);
                Assert.AreEqual(renewal.StorePluginOptions[0]!.FindPlugin(_plugin), plugin);
            }
        }

        [TestMethod]
        public void DeserializeStoreCorrectArray()
        {
            foreach (var plugin in _plugin.GetPlugins(Steps.Store))
            {
                var input = @$"{{
                              ""StorePluginOptions"": [{{
                                ""Plugin"": ""{plugin.Id}""
                              }}]
                            }}";
                var renewal = Deserialize(input);
                _log.Information(plugin.Runner.Name);
                Assert.IsInstanceOfType(renewal.StorePluginOptions[0], plugin.Meta.Options);
                Assert.AreEqual(renewal.StorePluginOptions[0]!.FindPlugin(_plugin), plugin);
            }
        }

        [TestMethod]
        public void DeserializeInstallationCorrectArray()
        {
            foreach (var plugin in _plugin.GetPlugins(Steps.Installation))
            {
                var input = @$"{{
                              ""InstallationPluginOptions"": [{{
                                ""Plugin"": ""{plugin.Id}""
                              }}]
                            }}";
                var renewal = Deserialize(input);
                _log.Information(plugin.Runner.Name);
                Assert.IsInstanceOfType(renewal.InstallationPluginOptions[0], plugin.Meta.Options);
                Assert.AreEqual(renewal.InstallationPluginOptions[0]!.FindPlugin(_plugin), plugin);
            }
        }
    }
}
