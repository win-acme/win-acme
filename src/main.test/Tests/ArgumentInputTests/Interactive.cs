using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Tests.ArgumentInputTests
{
    [TestClass]
    public class Interactive
    {
        [TestMethod]
        [DataRow("--centralsslstore I:", new[] { "" }, "I:", DisplayName = "ArgumentDefault")]
        [DataRow("--centralsslstore I:", new[] { "C:\\" }, "C:\\", DisplayName = "OverrideArgument")]
        [DataRow("", new[] { "C:\\" }, "C:\\", DisplayName = "NoArgument")]
        [DataRow("--centralsslstore", new[] { "C:\\" }, "C:\\", DisplayName = "EmptyArgument")]
        public void BasicString(
            string argument, 
            string[] userInput, 
            string output)
        {
            var container = new MockContainer().TestScope(
                userInput.ToList(), 
                commandLine: argument);
            var mock = container.Resolve<ArgumentsInputService>();
            var input = container.Resolve<IInputService>();
            var basic = mock.
                GetString<CentralSslArguments>(x => x.CentralSslStore).
                Interactive(input, "Label").
                GetValue().
                Result;
            Assert.AreEqual(output, basic);
        }

        /*
        [TestMethod]
        [DataRow("--pfxpassword 1234", true, "1234", DisplayName = "Normal")]
        [DataRow("", false, null, DisplayName = "Nothing")]
        [DataRow("", true, null, DisplayName = "AllowEmptyMissing")]
        [DataRow("--pfxpassword", true, "", DisplayName = "AllowEmptyNull")]
        [DataRow("--pfxpassword \"\"", true, "", DisplayName = "AllowEmptyEmpty")]
        public void BasicSecret(string commandLine, bool allowEmpty, string? output)
        {
            var container = new MockContainer().TestScope(commandLine: commandLine);
            var mock = container.Resolve<ArgumentsInputService>();
            var result = mock.GetProtectedString<CentralSslArguments>(x => x.PfxPassword, allowEmpty).
                GetValue().
                Result;
            Assert.AreEqual(output, result?.Value);
        }

        [TestMethod]
        [DataRow(null, "default", "default", DisplayName = "Base")]
        [DataRow("--centralsslstore a", "b", "a", DisplayName = "CommandLineBeatsDefault")]
        [DataRow("--centralsslstore a", null, "a", DisplayName = "CommandLineBeatsNullDefault")]
        [DataRow("--centralsslstore a", "", "a", DisplayName = "CommandLineBeatsEmptyDefault")]
        public void DefaultValue(string? commandLine, string @default, string output)
        {
            var container = new MockContainer().TestScope(commandLine: commandLine ?? "");
            var mock = container.Resolve<ArgumentsInputService>();
            var result = mock.GetString<CentralSslArguments>(x => x.CentralSslStore).
                WithDefault(@default).
                GetValue().
                Result;
            Assert.AreEqual(output, result);
        }

        [TestMethod]
        [DataRow("--centralsslstore a", "a", DisplayName = "Base")]
        [DataRow(null, null, DisplayName = "Nothing")]
        public void DefaultAsNullWithoutDefault(string? commandLine, string output)
        {
            var container = new MockContainer().TestScope(commandLine: commandLine ?? "");
            var mock = container.Resolve<ArgumentsInputService>();
            var result = mock.
                GetString<CentralSslArguments>(x => x.CentralSslStore).
                DefaultAsNull().
                GetValue().
                Result;
            Assert.AreEqual(output, result);
        }

        [TestMethod]
        [DataRow(null, "default", null, DisplayName = "Base")]
        [DataRow("--centralsslstore a", "b", "a", DisplayName = "CommandLineBeatsDefault")]
        [DataRow("--centralsslstore a", null, "a", DisplayName = "CommandLineBeatsNullDefault")]
        [DataRow("--centralsslstore a", "", "a", DisplayName = "CommandLineBeatsEmptyDefault")]
        [DataRow("--centralsslstore b", "b", null, DisplayName = "BaseDefaultNull")]
        public void DefaultAsNull(string? commandLine, string @default, string output)
        {
            var container = new MockContainer().TestScope(commandLine: commandLine ?? "");
            var mock = container.Resolve<ArgumentsInputService>();
            var result = mock.
                GetString<CentralSslArguments>(x => x.CentralSslStore).
                WithDefault(@default).
                DefaultAsNull().
                GetValue().
                Result;
            Assert.AreEqual(output, result);
        }

        [TestMethod]
        [DataRow(null, true, DisplayName = "Missing")]
        [DataRow("--centralsslstore a", false, DisplayName = "Provided")]
        public void Required(string? commandLine, bool shouldThrow)
        {
            var container = new MockContainer().TestScope(commandLine: commandLine ?? "");
            var mock = container.Resolve<ArgumentsInputService>();
            try
            {
                var result = mock.
                    GetString<CentralSslArguments>(x => x.CentralSslStore).
                    Required().
                    GetValue().
                    Result;
                Assert.AreEqual(shouldThrow, false, "No exception thrown though it should have been");
            }
            catch
            {
                Assert.AreEqual(shouldThrow, true, "Exception throw when it should not have been");
            }

        }

        [TestMethod]
        [DataRow(null, null, true, null, DisplayName = "EmptyInputAndDefault")]
        [DataRow(null, "a", false, "a", DisplayName = "NormalDefault")]
        [DataRow("--centralsslstore a", "b", false, "a", DisplayName = "CommandlineOverruleDefault")]
        [DataRow("--centralsslstore a", null, false, "a", DisplayName = "NullDefault")]
        [DataRow("--centralsslstore a", "", false, "a", DisplayName = "EmtpyDefault")]
        public void RequiredWithDefault(string? commandLine, string @default, bool shouldThrow, string expectedValue)
        {
            var container = new MockContainer().TestScope(commandLine: commandLine ?? "");
            var mock = container.Resolve<ArgumentsInputService>();
            try
            {
                var result = mock.
                    GetString<CentralSslArguments>(x => x.CentralSslStore).
                    WithDefault(@default).
                    Required().
                    GetValue().
                    Result;
                Assert.AreEqual(shouldThrow, false, "No exception thrown though it should have been");
                Assert.AreEqual(expectedValue, result);
            }
            catch
            {
                Assert.AreEqual(shouldThrow, true, "Exception throw when it should not have been");
            }
        }

        [TestMethod]
        [DataRow(null, null, true, null, DisplayName = "EmptyInputAndDefault")]
        [DataRow(null, "a", false, "a", DisplayName = "NormalDefault")]
        [DataRow("--centralsslstore a", "b", false, "a", DisplayName = "CommandlineOverruleDefault")]
        [DataRow("--centralsslstore a", null, false, "a", DisplayName = "NullDefault")]
        [DataRow("--centralsslstore a", "", false, "a", DisplayName = "EmtpyDefault")]
        public void RequiredWithDefaultReordered(string? commandLine, string @default, bool shouldThrow, string expectedValue)
        {
            var container = new MockContainer().TestScope(commandLine: commandLine ?? "");
            var mock = container.Resolve<ArgumentsInputService>();
            try
            {
                var result = mock.
                    GetString<CentralSslArguments>(x => x.CentralSslStore).
                    Required().
                    WithDefault(@default).
                    GetValue().
                    Result;
                Assert.AreEqual(shouldThrow, false, "No exception thrown though it should have been");
                Assert.AreEqual(expectedValue, result);
            }
            catch
            {
                Assert.AreEqual(shouldThrow, true, "Exception throw when it should not have been");
            }
        }

        [TestMethod]
        [DataRow("--centralsslstore valid", false, DisplayName = "Valid")]
        [DataRow("--centralsslstore invalid", true, DisplayName = "Invalid")]
        [DataRow("--centralsslstore \"\"", false, DisplayName = "Empty")]
        [DataRow("", false, DisplayName = "Null")]
        public void Validate(string input, bool shouldThrow)
        {
            var container = new MockContainer().TestScope(commandLine: input);
            var mock = container.Resolve<ArgumentsInputService>();
            try
            {
                var result = mock.
                    GetString<CentralSslArguments>(x => x.CentralSslStore).
                    Validate(x => x == "valid", "not valid").
                    GetValue().
                    Result;
                Assert.AreEqual(shouldThrow, false, "No exception thrown though it should have been");
            }
            catch
            {
                Assert.AreEqual(shouldThrow, true, "Exception throw when it should not have been");
            }
        }

        [TestMethod]
        [DataRow("--centralsslstore ab", false, DisplayName = "Valid")]
        [DataRow("--centralsslstore a", true, DisplayName = "OnlyA")]
        [DataRow("--centralsslstore b", true, DisplayName = "OnlyB")]
        [DataRow("--centralsslstore c", true, DisplayName = "Invalid")]
        [DataRow("--centralsslstore \"\"", false, DisplayName = "Empty")]
        [DataRow("", false, DisplayName = "Null")]
        public void ValidateMultiple(string input, bool shouldThrow)
        {
            var container = new MockContainer().TestScope(commandLine: input);
            var mock = container.Resolve<ArgumentsInputService>();
            try
            {
                var result = mock.
                    GetString<CentralSslArguments>(x => x.CentralSslStore).
                    Validate(x => x?.Contains("a") ?? false, "No A").
                    Validate(x => x?.Contains("b") ?? false, "No B").
                    GetValue().
                    Result;
                Assert.AreEqual(shouldThrow, false, "No exception thrown though it should have been");
            }
            catch
            {
                Assert.AreEqual(shouldThrow, true, "Exception throw when it should not have been");
            }
        }*/
    }
}
