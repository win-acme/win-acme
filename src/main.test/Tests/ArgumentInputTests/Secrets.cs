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

        [TestMethod]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "1" // "None"
            },
            "", // Expected output,
            DisplayName = "NoneMenu"
        )]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "2", // "Input from menu"
                ""
            },
            "", // Expected output,
            DisplayName = "EmptyInput"
        )]
        public void AllowEmtpy(string commandLine, string[] userInput, string? output)
        {
            var container = new MockContainer().TestScope(userInput.ToList(), commandLine);
            var input = container.Resolve<IInputService>();
            var mock = container.Resolve<ArgumentsInputService>();
            var secrets = container.Resolve<ISecretService>();
            var result = mock.GetProtectedString<CentralSslArguments>(x => x.PfxPassword, true).
                Interactive(input, "label").
                GetValue().
                Result;
            Assert.AreEqual(output, result?.Value);
        }

        [TestMethod]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "2", // "Type/paste in console"
                "1234", // The secret
                "y", // Store to vault
                "key3", // Key to use
            },
            "vault://mock/key3", // Expected output
            "1234",
            DisplayName = "NewEntry"
        )]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "2", // "Type/paste in console"
                "1234", // The secret
                "y", // Store to vault
                "key1", // Key to use
                "y" // Confirm overwrite
            },
            "vault://mock/key1", // Expected output
            "1234", // Expected secret
            DisplayName = "Overwrite"
        )]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "2", // "Type/paste in console"
                "1234", // The secret
                "y", // Store to vault
                "key1", // Key to use
                "n", // Abort overwrite
                "key3" // Alternative to use
            },
            "vault://mock/key3", // Expected output
            "1234", // Expected secret
            DisplayName = "CollisionAvoid"
        )]
        public void StoreInVault(string commandLine, string[] userInput, string? output, string secret)
        {
            var container = new MockContainer().TestScope(userInput.ToList(), commandLine);
            var input = container.Resolve<IInputService>();
            var mock = container.Resolve<ArgumentsInputService>();
            var secrets = container.Resolve<SecretServiceManager>();
            var result = mock.GetProtectedString<CentralSslArguments>(x => x.PfxPassword).
                Interactive(input, "label").
                GetValue().
                Result;
            Assert.AreEqual(output, result?.Value);

            var foundSecret = secrets.EvaluateSecret(result?.Value);
            Assert.AreEqual(secret, foundSecret);
        }

        [TestMethod]
        [DataRow(
            "", // Command line 
            new[] { // UserInput
                "3", // "Use from vault"
                "1" // Select key1
            },
            "vault://mock/key1", // Expected output
            "secret1",
            DisplayName = "Basic"
        )]
        public void UseFromVault(string commandLine, string[] userInput, string? output, string secret)
        {
            var container = new MockContainer().TestScope(userInput.ToList(), commandLine);
            var input = container.Resolve<IInputService>();
            var mock = container.Resolve<ArgumentsInputService>();
            var secrets = container.Resolve<SecretServiceManager>();
            var result = mock.GetProtectedString<CentralSslArguments>(x => x.PfxPassword).
                Interactive(input, "label").
                GetValue().
                Result;
            Assert.AreEqual(output, result?.Value);

            var foundSecret = secrets.EvaluateSecret(result?.Value);
            Assert.AreEqual(secret, foundSecret);
        }

    }
}
