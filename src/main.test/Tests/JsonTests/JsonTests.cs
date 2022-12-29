using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using PKISharp.WACS.UnitTests.Mock.Services;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace PKISharp.WACS.UnitTests.Tests.JsonTests
{
    [TestClass]
    public class JsonTests
    {
        private readonly ILifetimeScope _container;
        private readonly IPluginService _plugin;
        private readonly ILogService _log;

        public JsonTests()
        {
            var builder = new ContainerBuilder();
            _ = builder.RegisterType<MockSettingsService>().As<ISettingsService>();
            _ = builder.RegisterType<MockAssemblyService>().As<AssemblyService>();
            _ = builder.RegisterType<Mock.Services.LogService>().As<ILogService>();
            _ = builder.RegisterType<PluginService>().As<IPluginService>();
            _ = builder.Register(x =>
            {
                var context = x.Resolve<IComponentContext>();
                if (context is ILifetimeScope scope)
                {
                    return new WacsJsonOptionsFactory(scope);
                }
                throw new Exception();
            }).As<WacsJsonOptionsFactory>().SingleInstance();
            _ = builder.RegisterType<WacsJson>().SingleInstance();
            _ = builder.RegisterType<WacsJsonPluginsOptionsFactory>().SingleInstance();
            _ = builder.RegisterType<WacsJsonPlugins>().SingleInstance();
            _container = builder.Build();
            _plugin = _container.Resolve<IPluginService>();
            _log = _container.Resolve<ILogService>();
        }

        private Renewal Deserialize(string json)
        {
            var wacsJson = _container.Resolve<WacsJson>();
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
    }
}
