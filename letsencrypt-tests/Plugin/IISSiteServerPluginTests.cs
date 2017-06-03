using Microsoft.VisualStudio.TestTools.UnitTesting;
using letsencrypt;
using System;

using letsencrypt_tests.Support;
using letsencrypt.Support;
using System.IO;
using System.Reflection;

namespace letsencrypt_tests
{
    [TestClass()]
    [DeploymentItem("ACMESharp.PKI.Providers.OpenSslLib32.dll")]
    [DeploymentItem("ACMESharp.PKI.Providers.OpenSslLib64.dll")]
    [DeploymentItem("IISSiteServer.json")]
    [DeploymentItem("localhost22233-all.pfx")]
    [DeploymentItem("ManagedOpenSsl.dll")]
    [DeploymentItem("ManagedOpenSsl64.dll")]
    [DeploymentItem("Registration")]
    [DeploymentItem("Signer")]
    [DeploymentItem("test-cert.der")]
    [DeploymentItem("test-cert.pem")]
    [DeploymentItem("web_config.xml")]
    [DeploymentItem("x64\\libeay32.dll", "x64")]
    [DeploymentItem("x64\\ssleay32.dll", "x64")]
    [DeploymentItem("x86\\libeay32.dll", "x86")]
    [DeploymentItem("x86\\ssleay32.dll", "x86")]
    public class IISSiteServerPluginTests : TestBase
    {
        [TestInitialize]
        public override void Initialize()
        {
            StartHTTPProxy = true;
            StartFTPProxy = false;
            AllowInsecureSSLRequests = true;
            base.Initialize();
        }

        private void CreatePlugin(out IISSiteServerPlugin plugin, out Options options)
        {
            IISSiteServerPlugin.RegisterServerManager<MockIISServerManager>();
            plugin = new IISSiteServerPlugin();
            options = MockOptions();
            options.Plugin = R.IISSiteServer;
            options.San = true;
            options.CertOutPath = options.ConfigPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        [TestMethod()]
        public void IISSiteServerPlugin_ValidateTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            Assert.IsTrue(plugin.Validate(options));
        }

        [TestMethod()]
        public void IISSiteServerPlugin_GetSelectedTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            Assert.IsTrue(plugin.GetSelected(new ConsoleKeyInfo('s', ConsoleKey.S, false, false, false)));
            Assert.IsFalse(plugin.GetSelected(new ConsoleKeyInfo('q', ConsoleKey.Q, false, false, false)));
        }

        [TestMethod()]
        public void IISSiteServerPlugin_SelectOptionsTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.Validate(options);
            Assert.IsTrue(plugin.SelectOptions(options));
        }

        [TestMethod()]
        public void IISSiteServerPlugin_DeleteAuthorizationTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);

            var token = "this-is-a-test";
            var webRoot = "/";
            var challengeLocation = $"/.well-known/acme-challenge/{token}";
            var rootPath = plugin.BaseDirectory;
            var challengeFile = $"{rootPath}{challengeLocation}".Replace('/', Path.DirectorySeparatorChar);

            Directory.CreateDirectory(Path.GetDirectoryName(challengeFile));
            File.WriteAllText(challengeFile, token);
            plugin.DeleteAuthorization(options, rootPath + challengeLocation, token, webRoot, challengeLocation);
            Assert.IsFalse(File.Exists(challengeFile));
        }

        [TestMethod()]
        public void IISSiteServerPlugin_InstallTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            options.BaseUri = ProxyUrl("/");
            plugin.client = MockAcmeClient(options);
            var target = new Target
            {
                PluginName = R.IISSiteServer,
                Host = HTTPProxyServer,
                SiteId = 0,
                WebRootPath = plugin.BaseDirectory
            };
            plugin.SelectOptions(options);
            plugin.Install(target, options);
        }

        [TestMethod()]
        public void IISSiteServerPlugin_GetTargetsTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.SelectOptions(options);
            var targets = plugin.GetTargets(options);

            Assert.AreEqual(targets.Count, 1);
            Assert.AreEqual(targets[0].PluginName, R.IISSiteServer);
            Assert.AreEqual(targets[0].Host, null);
            Assert.AreEqual(targets[0].SiteId, 0);
        }

        [TestMethod()]
        public void IISSiteServerPlugin_PrintMenuTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            plugin.PrintMenu();
        }

        [TestMethod()]
        public void IISSiteServerPlugin_BeforeAuthorizeTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            var token = "this-is-a-test";
            var challengeLocation = $"/.well-known/acme-challenge/{token}";
            var rootPath = plugin.BaseDirectory;
            var target = new Target
            {
                PluginName = R.IIS,
                WebRootPath = rootPath,
                Host = HTTPProxyServer
            };
            plugin.BeforeAuthorize(target, rootPath + challengeLocation, token);
            var webconfigFile = Path.Combine(rootPath, ".well-known", "acme-challenge", "web.config");
            Assert.IsTrue(File.Exists(webconfigFile));
        }

        [TestMethod()]
        public void IISSiteServerPlugin_CreateAuthorizationFileTest()
        {
            IISSiteServerPlugin plugin;
            Options options;
            CreatePlugin(out plugin, out options);
            var token = "this-is-a-test";
            var rootPath = plugin.BaseDirectory;
            var challengeFile = Path.Combine(rootPath, ".well-known", "acme-challenge", token);
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