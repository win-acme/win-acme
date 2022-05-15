using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.UnitTests.Mock.Services;
using System.IO;

namespace PKISharp.WACS.UnitTests.Tests.InstallationPluginTests
{
    [TestClass]
    public class ScriptClientTests
    {
        private readonly LogService log;
        private FileInfo? psExit;

        public ScriptClientTests()
        {
            log = new LogService(false);
        }

        [TestMethod]
        [DataRow(0, true)]
        [DataRow(-1, false)]
        [DataRow(1, false)]
        public void TestEnvironmentExit(int exit, bool expectedSuccess)
        {
            var settings = new MockSettingsService();
            var tempPath = Infrastructure.Directory.Temp();
            psExit = new FileInfo(tempPath.FullName + "\\exit.ps1");
            File.WriteAllText(psExit.FullName, $"[Environment]::Exit({exit})");
            var sc = new Clients.ScriptClient(log, settings);
            var success = sc.RunScript(psExit.FullName, "", "").Result;
            Assert.AreEqual(expectedSuccess, success);
        }

        [TestMethod]
        [DataRow(0, true)]
        [DataRow(-1, false)]
        [DataRow(1, false)]
        public void TestExit(int exit, bool expectedSuccess)
        {
            var settings = new MockSettingsService();
            var tempPath = Infrastructure.Directory.Temp();
            psExit = new FileInfo(tempPath.FullName + "\\exit.ps1");
            File.WriteAllText(psExit.FullName, $"exit {exit}");
            var sc = new Clients.ScriptClient(log, settings);
            var success = sc.RunScript(psExit.FullName, "", "").Result;
            Assert.AreEqual(expectedSuccess, success);
        }

        [TestMethod]
        public void TestException()
        {
            var settings = new MockSettingsService();
            var tempPath = Infrastructure.Directory.Temp();
            psExit = new FileInfo(tempPath.FullName + "\\exit.ps1");
            File.WriteAllText(psExit.FullName, $"throw 'error'");
            var sc = new Clients.ScriptClient(log, settings);
            var success = sc.RunScript(psExit.FullName, "", "").Result;
            Assert.AreEqual(false, success);
        }

        [TestMethod]
        public void TestExceptionCatch()
        {
            var settings = new MockSettingsService();
            var tempPath = Infrastructure.Directory.Temp();
            psExit = new FileInfo(tempPath.FullName + "\\exit.ps1");
            File.WriteAllText(psExit.FullName, $"try {{ throw 'error' }} catch {{ }}");
            var sc = new Clients.ScriptClient(log, settings);
            var success = sc.RunScript(psExit.FullName, "", "").Result;
            Assert.AreEqual(true, success);
        }
    }
}
