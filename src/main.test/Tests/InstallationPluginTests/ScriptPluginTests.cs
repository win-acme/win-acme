using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock;
using PKISharp.WACS.UnitTests.Mock.Services;
using System;
using System.Collections.Generic;
using System.IO;

namespace PKISharp.WACS.UnitTests.Tests.InstallationPluginTests
{
    [TestClass]
    public class ScriptPluginTests
    {
        private readonly Mock.Services.LogService log;
        private readonly ICertificateService cs;
        private readonly FileInfo batchPath;
        private readonly FileInfo batchPsPath;
        private readonly FileInfo psPath;
        private readonly FileInfo psNamedPath;
        private readonly FileInfo psTrickyPath;
        private readonly FileInfo psMulti;

        public ScriptPluginTests()
        {
           
            log = new Mock.Services.LogService(false);
            cs = new Mock.Services.CertificateService();
            
            var tempPath = Infrastructure.Directory.Temp();
            batchPath = new FileInfo(tempPath.FullName + "\\create.bat");
            File.WriteAllText(batchPath.FullName, "echo hello %1");

            batchPsPath = new FileInfo(tempPath.FullName + "\\runps.bat");
            File.WriteAllText(batchPsPath.FullName, "powershell.exe -ExecutionPolicy ByPass -File %*");

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

            psTrickyPath = new FileInfo(tempPath.FullName + "\\create space and ' quote.ps1");
            File.WriteAllText(psTrickyPath.FullName,
                $"Write-Host \"Hello\""
            );

            psMulti = new FileInfo(tempPath.FullName + "\\real.ps1");
            File.WriteAllText(psMulti.FullName,
                $"param(" +
                $"  [Parameter(Mandatory)][string]$Param1,\n" +
                $"  [Parameter(Mandatory)][string]$Param2\n" +
                $")\n" +
                $"if ($Param1 -ne \"param1 param1\") " +
                $"{{ " +
                $"  Write-Error \"Wrong: $Param1\" " +
                $"}} else {{ " +
                $"  Write-Host \"Hello $Param1\"" +
                $"}}" + 
                $"if ($Param2 -ne \"param2 param2\") " +
                $"{{ " +
                $"  Write-Error \"Wrong: $Param2\" " +
                $"}} else {{ " +
                $"  Write-Host \"Hello $Param2\"" +
                $"}}"
            );

        }

        private void TestScript(string script, bool psCore, string? parameters)
        {
            var renewal = new Renewal();
            var settings = new MockSettingsService();
            if (psCore)
            {
                settings.Script.PowershellExecutablePath = "C:\\Program Files\\PowerShell\\7\\pwsh.exe";
            }
            var target = new Target("", "test.local", new List<TargetPart>());
            var targetOrder = new Order(renewal, target);
            var oldCert = cs.RequestCertificate(null, targetOrder).Result;
            var newCert = cs.RequestCertificate(null, targetOrder).Result;
            var storeInfo = new Dictionary<Type, StoreInfo>
            {
                { typeof(CertificateStore), new StoreInfo() { } }
            };
            var options = new ScriptOptions
            {
                Script = script,
                ScriptParameters = parameters
            };
            var container = new MockContainer().TestScope();
            var installer = new Script(renewal, options, new Clients.ScriptClient(log, settings), container.Resolve<SecretServiceManager>());
            installer.Install(storeInfo, newCert, oldCert).Wait();
        }

        [TestMethod]
        public void BatRegular()
        {
            TestScript(batchPath.FullName, false, null);
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        public void BatWithParams()
        {
            TestScript(batchPath.FullName, false, "-world");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        public void BatWithSingleQuoteParams()
        {
            TestScript(batchPath.FullName, false, "'-world 2'");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        public void BatWithDoubleQuoteParams()
        {
            TestScript(batchPath.FullName, false, "\"-world 2\"");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        public void BatWithPs()
        {
            TestScript(batchPsPath.FullName, false, psPath.FullName);
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void Ps1Regular(bool psCore)
        {
            TestScript(psPath.FullName, psCore, null);
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void Ps1Tricky(bool psCore)
        {
            TestScript(psTrickyPath.FullName, psCore, null);
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void Ps1WithParams(bool psCore)
        {
            TestScript(psPath.FullName, psCore, "world");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void Ps1WithSingleQuoteParams(bool psCore)
        {
            TestScript(psPath.FullName, psCore, "'world'");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void Ps1WithDoubleQuoteParams(bool psCore)
        {
            TestScript(psPath.FullName, psCore, "\"world\"");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void Ps1WithError(bool psCore)
        {
            TestScript(psPath.FullName, psCore, "world2");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(!log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void Ps1NamedWrong(bool psCore)
        {
            TestScript(psNamedPath.FullName, psCore, "-wrong 'world'");
            Assert.IsTrue(!log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void Ps1NamedCorrect(bool psCore)
        {
            TestScript(psNamedPath.FullName, psCore, "-what 'world'");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }

        [TestMethod]
        [DataRow(true, DisplayName = "PScore")]
        [DataRow(false, DisplayName = "PSclassic")]
        public void PsRealworld(bool psCore)
        {
            TestScript(psMulti.FullName, psCore, " -Param1 'param1 param1' -Param2 \"param2 param2\"");
            Assert.IsTrue(log.WarningMessages.IsEmpty);
            Assert.IsTrue(log.ErrorMessages.IsEmpty);
        }


    }
}
