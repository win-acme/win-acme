using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using letsencrypt;
using letsencrypt.Support;
using letsencrypt_tests.Support;

namespace letsencrypt_tests
{
    [TestClass]
    public class LetsEncryptTests : TestBase
    {
        [TestMethod]
        public void CreateAcmeClient_Test()
        {
            var options = MockOptions();
            Assert.IsNotNull(LetsEncrypt.CreateAcmeClient(options));
        }

        [TestMethod]
        public void SelectPlugin_Test()
        {
            var options = MockOptions();
            options.Plugin = R.AzureWebApp;
            var plugin = LetsEncrypt.SelectPlugin(options);
            Assert.IsNotNull(plugin);
            Assert.IsInstanceOfType(plugin, typeof(AzureWebAppPlugin));
        }
    }
}
