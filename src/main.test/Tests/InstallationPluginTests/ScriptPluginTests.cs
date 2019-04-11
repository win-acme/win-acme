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
        private Mock.Services.LogService log;
        private readonly IIISClient iis;
        private readonly ICertificateService cs;
        private readonly FileInfo batchPath;
        private readonly FileInfo psPath;
        private readonly FileInfo psNamedPath;

        public ScriptPluginTests()
        {
            log = new Mock.Services.LogService(true);
            iis = new Mock.Clients.MockIISClient();
            cs = new Mock.Services.CertificateService();
            var tempPath = Infrastructure.Directory.Temp();
            batchPath = new FileInfo(tempPath.FullName + "\\create.bat");
            File.WriteAllText(batchPath.FullName, "echo hello %1");

            psPath = new FileInfo(tempPath.FullName + "\\create.ps1");
            File.WriteAllText(psPath.FullName, 
                $"$arg = $($args[0])\n" +
                $"if ($arg -ne $null -and $arg -ne \"world\") " +
                $"{{ " +
                $"  Write-Error \"Wrong: $arg\" " +
                $"}} else {{" +
                $"  Write-Host \"Hello $arg\" " +
                $"}}"
            );

            psNamedPath = new FileInfo(tempPath.FullName + "\\createnamed.ps1");
            File.WriteAllText(psNamedPath.FullName,
                $"param([Parameter(Mandatory)][string]$What)\n" +
                $"if ($What -ne \"world\") " +
                $"{{ " +
                $"  Write-Error \"Wrong: $What\" " +
                $"}} else {{ " +
                $"  Write-Host \"Hello $What\"" +
                $"}}"
            );

        }

        private void TestScript(string script, string parameters)
        {
            log = new Mock.Services.LogService(true);
            var renewal = new Renewal();
            var storeOptions = new CertificateStoreOptions();
            var store = new CertificateStore(log, iis, storeOptions);
            var oldCert = cs.RequestCertificate(null, RunLevel.Unattended, renewal, new Target() { CommonName = "test.local" }, null);
            var newCert = cs.RequestCertificate(null, RunLevel.Unattended, renewal, new Target() { CommonName = "test.local" }, null);
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
            TestScript(batchPath.FullName, "-world");
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 0);
        }

        [TestMethod]
        public void BatWithSingleQuoteParams()
        {
            TestScript(batchPath.FullName, "'-world 2'");
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 0);
        }

        [TestMethod]
        public void BatWithDoubleQuoteParams()
        {
            TestScript(batchPath.FullName, "\"-world 2\"");
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
        public void Ps1WithSingleQuoteParams()
        {
            TestScript(psPath.FullName, "'world'");
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 0);
        }

        [TestMethod]
        public void Ps1WithDoubleQuoteParams()
        {
            TestScript(psPath.FullName, "\"world\"");
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

        [TestMethod]
        public void Ps1NamedWrong()
        {
            TestScript(psNamedPath.FullName, "-wrong 'world'");
            Assert.IsTrue(log.ErrorMessages.Count == 2);
        }

        [TestMethod]
        public void Ps1NamedCorrect()
        {
            TestScript(psNamedPath.FullName, "-what 'world'");
            Assert.IsTrue(log.WarningMessages.Count == 0);
            Assert.IsTrue(log.ErrorMessages.Count == 0);
        }


    }
}
