using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock;
using real = PKISharp.WACS.Services;

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
            var secretService = _container.Resolve<real.ISecretService>();
            secretService.PutSecret(theKey, theSecret);
        }

        [TestMethod]
        public void Direct()
        {
            var secondSecret = _container!.Resolve<real.ISecretService>();
            var restoredSecret = secondSecret.GetSecret(theKey);
            Assert.AreEqual(theSecret, restoredSecret);
        }

        [TestMethod]
        public void ThroughManager()
        {
            var secretService = _container!.Resolve<real.ISecretService>();
            var manager = _container!.Resolve<real.SecretServiceManager>();
            var restoredSecret = manager.EvaluateSecret($"{SecretServiceManager.VaultPrefix}{secretService.Prefix}/{theKey}");
            Assert.AreEqual(theSecret, restoredSecret);
        }

    }
}
