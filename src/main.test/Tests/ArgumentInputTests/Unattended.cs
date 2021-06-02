using Autofac;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using PKISharp.WACS.UnitTests.Mock;
using System.Threading.Tasks;

namespace PKISharp.WACS.UnitTests.Tests.ArgumentInputTests
{
    [TestClass]
    public class Unattended
    {
        [TestMethod]
        [DataRow("I:", "I:", DisplayName = "Base")]
        [DataRow("I:\\", "I:\\", DisplayName = "WithSlash")]
        [DataRow("\"I:\\\"", "I:\\", DisplayName = "WithDoubleQuotes")]
        [DataRow("'I:\\'", "'I:\\'", DisplayName = "WithSingleQuotes")]
        public void BasicString(string input, string output)
        {
            var container = new MockContainer().TestScope(commandLine: $"--centralsslstore {input}");
            var mock = container.Resolve<ArgumentsInputService>();
            var basic = mock.
                GetString<CentralSslArguments>(x => x.CentralSslStore).
                GetValue().
                Result;
            Assert.AreEqual(output, basic);
        }

        [TestMethod]
        [DataRow("4", 4)]
        public void BasicLong(string input, long output)
        {
            var container = new MockContainer().TestScope(commandLine: $"--installationsiteid {input}");
            var mock = container.Resolve<ArgumentsInputService>();
            var basic = mock.
                GetLong<IISWebArguments>(x => x.InstallationSiteId).
                GetValue().
                Result;
            Assert.AreEqual(output, basic);
        }

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
        [DataRow("--pfxpassword 1234", true, "default", "1234", DisplayName = "Normal")]
        [DataRow("", false, "default", "default", DisplayName = "Nothing")]
        [DataRow("", true, "default", "default", DisplayName = "AllowEmptyMissing")]
        [DataRow("--pfxpassword", true, "default", "", DisplayName = "AllowEmptyNull")]
        [DataRow("--pfxpassword \"\"", true, "default", "", DisplayName = "AllowEmptyEmpty")]
        [DataRow("--pfxpassword", false, "default", "default", DisplayName = "AllowEmptyNull")]
        [DataRow("--pfxpassword \"\"", false, "default", "default", DisplayName = "AllowEmptyEmpty")]
        public void SecretWithDefault(string commandLine, bool allowEmpty, string defaultValue, string? output)
        {
            var container = new MockContainer().TestScope(commandLine: commandLine);
            var mock = container.Resolve<ArgumentsInputService>();
            var result = mock.GetProtectedString<CentralSslArguments>(x => x.PfxPassword, allowEmpty).
                WithDefault(defaultValue.Protect(allowEmpty)).
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
                    Validate(x => Task.FromResult(x == "valid"), "not valid").
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
                    Validate(x => Task.FromResult(x?.Contains("a") ?? false), "No A").
                    Validate(x => Task.FromResult(x?.Contains("b") ?? false), "No B").
                    GetValue().
                    Result;
                Assert.AreEqual(shouldThrow, false, "No exception thrown though it should have been");
            }
            catch
            {
                Assert.AreEqual(shouldThrow, true, "Exception throw when it should not have been");
            }
        }
    }
}
