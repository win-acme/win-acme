using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Clients;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Real = PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Tests.SecretServiceTests
{
    [TestClass]
    public class SecretServiceTests
    {
        private ILifetimeScope? _container;
        private const string theKey = "test";
        private const string theSecret = "secret";

        [TestInitialize]
        public void Init()
        {
            _container = new MockContainer().TestScope();
            var secretService = _container.Resolve<Real.ISecretService>();
            secretService.PutSecret(theKey, theSecret);
        }

        [TestMethod]
        public void Direct()
        {
            var secondSecret = _container!.Resolve<Real.ISecretService>();
            var restoredSecret = secondSecret.GetSecret(theKey);
            Assert.AreEqual(theSecret, restoredSecret);
        }

        [TestMethod]
        public void ThroughManager()
        {
            var secretService = _container!.Resolve<Real.ISecretService>();
            var manager = _container!.Resolve<Real.SecretServiceManager>();
            var restoredSecret = manager.EvaluateSecret($"{SecretServiceManager.VaultPrefix}{secretService.Prefix}/{theKey}");
            Assert.AreEqual(theSecret, restoredSecret);
        }

        [TestMethod]
        public void AsScriptParameter()
        {
            var secretService = _container!.Resolve<Real.ISecretService>();
            var secretServiceManager = _container!.Resolve<SecretServiceManager>();
            var scriptClient = _container!.Resolve<ScriptClient>();
            var scriptInstaller = new Script(new DomainObjects.Renewal(), new ScriptOptions(), scriptClient, secretServiceManager);
            var ecdsa = ECDsa.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"CN=dummy.com", ecdsa, HashAlgorithmName.SHA256);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
            var placeholder = $"{{{SecretServiceManager.VaultPrefix}{secretService.Prefix}/{theKey}}}";
            var output = scriptInstaller.ReplaceParameters(
                placeholder, 
                null, 
                new DomainObjects.CertificateInfo(cert), 
                null, 
                false);
            Assert.AreEqual(theSecret, output);

            var outputCensor = scriptInstaller.ReplaceParameters(
                placeholder,
                null,
                new DomainObjects.CertificateInfo(cert),
                null,
                true);
            Assert.AreEqual(placeholder, outputCensor);
        }
    }
}
