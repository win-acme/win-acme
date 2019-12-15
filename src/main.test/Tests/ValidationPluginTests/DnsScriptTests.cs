using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.Collections.Generic;
using System.IO;

namespace PKISharp.WACS.UnitTests.Tests.ValidationPluginTests
{
    [TestClass]
    public class DnsScriptTests
    {
        private readonly ILogService log;
        private readonly IPluginService plugins;
        private readonly FileInfo commonScript;
        private readonly FileInfo deleteScript;
        private readonly FileInfo createScript;

        public DnsScriptTests()
        {
            log = new Mock.Services.LogService(false);
            plugins = new MockPluginService(log);
            var tempPath = Infrastructure.Directory.Temp();
            commonScript = new FileInfo(tempPath.FullName + "\\dns-common.bat");
            File.WriteAllText(commonScript.FullName, "");
            deleteScript = new FileInfo(tempPath.FullName + "\\dns-delete.bat");
            File.WriteAllText(deleteScript.FullName, "");
            createScript = new FileInfo(tempPath.FullName + "\\dns-create.bat");
            File.WriteAllText(createScript.FullName, "");
        }

        private ScriptOptions Options(string commandLine)
        {
            var optionsParser = new ArgumentsParser(log, plugins, commandLine.Split(' '));
            var arguments = new ArgumentsService(log, optionsParser);
            var x = new ScriptOptionsFactory(log, arguments);
            var target = new Target("", "", new List<TargetPart>());
            return x.Default(target).Result;
        }

        [TestMethod]
        public void OnlyCommon()
        {
            var options = Options($"--dnsscript {commonScript.FullName}");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.Script, commonScript.FullName);
            Assert.IsNull(options.CreateScript, commonScript.FullName);
            Assert.IsNull(options.DeleteScript, commonScript.FullName);
        }

        [TestMethod]
        public void AutoMerge()
        {
            var options = Options($"--dnsdeletescript {commonScript.FullName} --dnscreatescript {commonScript.FullName}");
            Assert.IsNotNull(options);
            Assert.AreEqual(options.Script, commonScript.FullName);
            Assert.IsNull(options.CreateScript, commonScript.FullName);
            Assert.IsNull(options.DeleteScript, commonScript.FullName);
        }

        [TestMethod]
        public void Different()
        {
            var options = Options($"--dnsdeletescript {deleteScript.FullName} --dnscreatescript {createScript.FullName}");
            Assert.IsNotNull(options);
            Assert.IsNull(options.Script);
            Assert.AreEqual(options.CreateScript, createScript.FullName);
            Assert.AreEqual(options.DeleteScript, deleteScript.FullName);
        }

        [TestMethod]
        public void CreateOnly()
        {
            var options = Options($"--dnscreatescript {createScript.FullName}");
            Assert.IsNotNull(options);
            Assert.IsNull(options.Script);
            Assert.AreEqual(options.CreateScript, createScript.FullName);
            Assert.IsNull(options.DeleteScript, deleteScript.FullName);
        }

        [TestMethod]
        public void WrongPath()
        {
            var options = Options($"--dnscreatescript {createScript.FullName}error");
            Assert.IsNull(options);
        }
    }
}