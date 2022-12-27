using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Text.Json;

namespace PKISharp.WACS.UnitTests.Tests.JsonTests
{
    [TestClass]
    public class JsonTests
    {
        private readonly ISettingsService _settings;
        private readonly ILogService _log;
        private readonly IPluginService _plugin;

        public JsonTests()
        {
            _settings = new MockSettingsService();
            _log = new Mock.Services.LogService(false);
            var assembly = new MockAssemblyService(_log);
            _plugin = new PluginService(_log, assembly);
        }

        private Renewal Deserialize(string json)
        {
           
            var renewal = JsonSerializer.Deserialize(json, WacsJson.Convert(_plugin, _log, _settings).Renewal);
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
    }
}
