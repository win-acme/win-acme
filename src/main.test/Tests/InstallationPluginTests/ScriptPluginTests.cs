using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.IO;

namespace PKISharp.WACS.UnitTests.Tests.InstallationPluginTests
{
    [TestClass]
    public class ScriptPluginTests
    {
        private readonly Mock.Services.LogService log;
        private readonly IIISClient iis;
        private readonly ICertificateService cs;
        private readonly FileInfo batchPath;
        private readonly FileInfo psPath;

        public ScriptPluginTests()
        {
            log = new Mock.Services.LogService(true);
            iis = new Mock.Clients.MockIISClient();
            cs = new Mock.Services.CertificateService();
            var tempPath = new DirectoryInfo(Environment.ExpandEnvironmentVariables("%TEMP%\\wacs"));
            if (!tempPath.Exists)
            {
                tempPath.Create();
            }
            batchPath = new FileInfo(tempPath.FullName + "\\create.bat");
            File.WriteAllText(batchPath.FullName, "echo hello %1");

            psPath = new FileInfo(tempPath.FullName + "\\create.ps1");
            File.WriteAllText(psPath.FullName, 
                $"$arg = $($args[0])\n" +
                $"if ($arg -ne $null -and $arg -ne \"world\") {{ Write-Error \"Wrong\" }}\n" +
                $"Write-Host \"Hello $arg\""
            );

        }

        private void TestScript(string script, string parameters)
        {
            var renewal = new Renewal();
            var storeOptions = new CertificateStoreOptions();
            var store = new CertificateStore(log, iis, storeOptions);
            var oldCert = cs.RequestCertificate(null, renewal, new Target() { CommonName = "test.local" }, null);
            var newCert = cs.RequestCertificate(null, renewal, new Target() { CommonName = "test.local" }, null);
            var options = new ScriptOptions
            {
                Script = script,
                ScriptParameters = parameters
            };
            var installer = new Script(renewal, options, log);
            installer.Install(store, newCert, oldCert);
        }

        [TestMethod]
        public void BatRegular()
        {
            TestScript(batchPath.FullName, null);
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 0);
        }

        [TestMethod]
        public void BatWithParams()
        {
            TestScript(batchPath.FullName, "world");
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 0);
        }

        [TestMethod]
        public void Ps1Regular()
        {
            TestScript(psPath.FullName, null);
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 0);
        }

        [TestMethod]
        public void Ps1WithParams()
        {
            TestScript(psPath.FullName, "world");
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 0);
        }

        [TestMethod]
        public void Ps1WithError()
        {
            TestScript(psPath.FullName, "world2");
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 1);
        }

    }
}
