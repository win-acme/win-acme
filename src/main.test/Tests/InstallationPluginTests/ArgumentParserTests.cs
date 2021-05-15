using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System;

namespace PKISharp.WACS.UnitTests.Tests.InstallationPluginTests
{
    [TestClass]
    public class ArgumentParserTests
    {
        private readonly Mock.Services.LogService log;

        public ArgumentParserTests()
        {
            log = new Mock.Services.LogService(true);
        }

        private string? TestScript(string parameters)
        {
            var argParser = new ArgumentsParser(log, new MockPluginService(log),
                $"--scriptparameters {parameters} --verbose".Split(' '));
            var args = argParser.GetArguments<ScriptArguments>();
            return args?.ScriptParameters;
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void Illegal() => TestScript("hello nonsense");

        [TestMethod]
        public void SingleParam() => Assert.AreEqual("hello", TestScript("hello"));

        [TestMethod]
        public void SingleParamExtra() => Assert.AreEqual("hello", TestScript("hello --verbose"));

        [TestMethod]
        public void MultipleParams() => Assert.AreEqual("hello world", TestScript("\"hello world\""));

        [TestMethod]
        public void MultipleParamsExtra() => Assert.AreEqual("hello world", TestScript("\"hello world\" --test --verbose"));

        [TestMethod]
        public void MultipleParamsDoubleQuotes() => Assert.AreEqual("\"hello world\"", TestScript("\"\"hello world\"\""));

        [TestMethod]
        public void MultipleParamsDoubleQuotesExtra() => Assert.AreEqual("\"hello world\"", TestScript("\"\"hello world\"\" --test --verbose"));

        [TestMethod]
        public void MultipleParamsSingleQuotes() => Assert.AreEqual("'hello world'", TestScript("\"'hello world'\""));


        [TestMethod]
        public void EmbeddedKeySingle() => Assert.AreEqual("'hello --world'", TestScript("\"'hello --world'\""));

        [TestMethod]
        public void EmbeddedKeyDouble() => Assert.AreEqual("\"hello --world\"", TestScript("\"\"hello --world\"\""));

        [TestMethod]
        public void Real() => Assert.AreEqual("'{CertThumbprint}' 'IIS,SMTP,IMAP' '1' '{CacheFile}' '{CachePassword}' '{CertFriendlyName}'", TestScript("\"'{CertThumbprint}' 'IIS,SMTP,IMAP' '1' '{CacheFile}' '{CachePassword}' '{CertFriendlyName}'\""));
    }
}
