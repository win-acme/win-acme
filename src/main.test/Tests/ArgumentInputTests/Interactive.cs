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

        [TestMethod]
        [DataRow(null, new[] { "" }, "default", "default", DisplayName = "DefaultWorks")]
        [DataRow(null, new[] { "user" }, "default", "user", DisplayName = "UserBeatsDefault")]
        [DataRow("--centralsslstore command", new[] { "user" }, "default", "user", DisplayName = "UserBeatsCommandAndDefault")]
        [DataRow("--centralsslstore command", new[] { "" }, "default", "command", DisplayName = "CommandBeatsDefault")]
        [DataRow("--centralsslstore command", new[] { "" }, null, "command", DisplayName = "CommandBecomesDefault")]
        public void DefaultValue(string? commandLine, string[] userInput, string @default, string output)
        {
            var container = new MockContainer().TestScope(userInput.ToList(), commandLine ?? "");
            var mock = container.Resolve<ArgumentsInputService>();
            var input = container.Resolve<IInputService>(); 
            var result = mock.GetString<CentralSslArguments>(x => x.CentralSslStore).
                Interactive(input, "label").
                WithDefault(@default).
                GetValue().
                Result;
            Assert.AreEqual(output, result);
        }
    }
}
