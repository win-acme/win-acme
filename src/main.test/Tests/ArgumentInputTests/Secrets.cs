using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.ArgumentInputTests
{
    [TestClass]
    public class Secrets
    {
        [TestMethod]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "2", // "Type/paste in console"
                "1234", // The secret
                "n", // Do not store to vault
            }, 
            "1234", // Expected output
            DisplayName = "Normal")]
        public void BasicSecret(string commandLine, string[] userInput, string? output)
        {
            var container = new MockContainer().TestScope(userInput.ToList(), commandLine);
            var input = container.Resolve<IInputService>();
            var mock = container.Resolve<ArgumentsInputService>();
            var secrets = container.Resolve<ISecretService>();
            var result = mock.GetProtectedString<CentralSslArguments>(x => x.PfxPassword).
                Interactive(input, "label").
                GetValue().
                Result;
            Assert.AreEqual(output, result?.Value);
        }

        [TestMethod]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "4" // "Default"
            }, 
            "1234", // Default value
            "1234", // Expected output
            DisplayName = "DefaultNormal")]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "1" // "None"
            },
            "1234", // Default value
            "", // Expected output
            DisplayName = "ChooseNoneWithDefaultPresent")]
        public void DefaultValue(string commandLine, string[] userInput, string? defaultValue, string? output)
        {
            var container = new MockContainer().TestScope(userInput.ToList(), commandLine);
            var input = container.Resolve<IInputService>();
            var mock = container.Resolve<ArgumentsInputService>();
            var secrets = container.Resolve<ISecretService>();
            var result = mock.GetProtectedString<CentralSslArguments>(x => x.PfxPassword, true).
                Interactive(input, "label").
                WithDefault(defaultValue.Protect()).
                GetValue().
                Result;
            Assert.AreEqual(output, result?.Value);
        }


    }
}
