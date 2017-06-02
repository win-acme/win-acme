using Microsoft.VisualStudio.TestTools.UnitTesting;
using letsencrypt;
using System;

using letsencrypt_tests.Support;
using letsencrypt.Support;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;

namespace letsencrypt_tests
{
    [TestClass()]
    [DeploymentItem("ACMESharp.PKI.Providers.OpenSslLib32.dll")]
    [DeploymentItem("ACMESharp.PKI.Providers.OpenSslLib64.dll")]
    [DeploymentItem("localhost22233-all.pfx")]
    [DeploymentItem("ManagedOpenSsl.dll")]
    [DeploymentItem("ManagedOpenSsl64.dll")]
    [DeploymentItem("Manual.json")]
    [DeploymentItem("Registration")]
    [DeploymentItem("Signer")]
    [DeploymentItem("test-cert.der")]
    [DeploymentItem("test-cert.pem")]
    [DeploymentItem("web_config.xml")]
    [DeploymentItem("x64\\libeay32.dll", "x64")]
    [DeploymentItem("x64\\ssleay32.dll", "x64")]
    [DeploymentItem("x86\\libeay32.dll", "x86")]
    [DeploymentItem("x86\\ssleay32.dll", "x86")]
    public class ManualPluginTests : TestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            StartHTTPProxy = true;
            StartFTPProxy = false;
            AllowInsecureSSLRequests = true;
            base.Initialize();
        }

        private void CreatePlugin(out ManualPlugin plugin, out Options options)
        {
            plugin = new ManualPlugin();
            AzureRestApi.ApiRootUrl =
            AzureRestApi.AuthRootUrl = removeLastSlash(ProxyUrl("/"));
            options = MockOptions();
            options.Plugin = R.Manual;
            options.CertOutPath = options.ConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [TestMethod()]
        public void ManualPlugin_ValidateTest()
        {
            ManualPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            Assert.IsTrue(plugin.Validate(options));
        }

        [TestMethod()]
        public void ManualPlugin_GetSelectedTest()
        {
            ManualPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            Assert.IsTrue(plugin.GetSelected(new ConsoleKeyInfo('m', ConsoleKey.M, false, false, false)));
            Assert.IsFalse(plugin.GetSelected(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false)));
        }

        [TestMethod()]
        public void ManualPlugin_SelectOptionsTest()
        {
            ManualPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.Validate(options);
            Assert.IsTrue(plugin.SelectOptions(options));
        }
        
        [TestMethod()]
        public void ManualPlugin_InstallTest()
        {
            ManualPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.hostName = HTTPProxyServer;
            plugin.localPath = plugin.BaseDirectory;
            options.BaseUri = ProxyUrl("/");
            plugin.client = MockAcmeClient(options);
            var target = new Target
            {
                PluginName = R.Manual,
                Host = HTTPProxyServer,
                WebRootPath = plugin.BaseDirectory
            };
            plugin.Install(target, options);
        }

        [TestMethod()]
        public void ManualPlugin_GetTargetsTest()
        {
            ManualPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.hostName = "localhost";
            var rootPath = plugin.BaseDirectory;
            plugin.localPath = rootPath;
            var targets = plugin.GetTargets(options);

            Assert.AreEqual(targets.Count, 1);
            Assert.AreEqual(targets[0].PluginName, R.Manual);
            Assert.AreEqual(targets[0].Host, "localhost");
            Assert.AreEqual(targets[0].WebRootPath, rootPath);
        }

        [TestMethod()]
        public void ManualPlugin_PrintMenuTest()
        {
            ManualPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.PrintMenu();
        }

        [TestMethod()]
        public void ManualPlugin_BeforeAuthorizeTest()
        {
            ManualPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.hostName = HTTPProxyServer;
            plugin.localPath = plugin.BaseDirectory;

            var webRoot = "/";
            var token = "this-is-a-test";
            var challengeLocation = $"/.well-known/acme-challenge/{token}";
            var rootPath = $"{FTPServerUrl}{webRoot}";
            var target = new Target
            {
                PluginName = R.Manual,
                Host = plugin.hostName,
                WebRootPath = rootPath
            };
            plugin.BeforeAuthorize(target, rootPath + challengeLocation, token);
        }

        [TestMethod()]
        public void ManualPlugin_CreateAuthorizationFileTest()
        {
            ManualPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.hostName = HTTPProxyServer;
            var rootPath = plugin.BaseDirectory;
            plugin.localPath = rootPath;
            
            var token = "this-is-a-test";
            var challengeLocation = $"/.well-known/acme-challenge/{token}";
            var challengeFile = $"{MockFtpServer.localPath}{challengeLocation}".Replace('/', Path.DirectorySeparatorChar);
            plugin.CreateAuthorizationFile(challengeFile, token);
            Assert.IsTrue(File.Exists(challengeFile));
        }

        [TestCleanup]
        public override void Cleanup()
        {
            base.Cleanup();
        }
    }
}