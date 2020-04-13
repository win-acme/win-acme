using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Extensions;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.UnitTests.Tests.TargetPluginTests
{
    [TestClass]
    public class PatternTests
    {
        public PatternTests()
        {
        }

        public void RegexPattern(string input, string match, bool shouldSucceed)
        {
            var pattern = input.PatternToRegex();
            var regex = new Regex(pattern);
            Assert.AreEqual(regex.Match(match).Success, shouldSucceed);
        }

        [DataRow("e?", "ee", true)]
        [DataRow("e?", "eee", false)]
        [DataRow("e?", "e?", true)]
        [TestMethod]
        public void RegularQuestion(string input, string match, bool shouldSucceed) => RegexPattern(input, match, shouldSucceed);
        
        [DataRow("e?", "ee", false)]
        [DataRow("e?", "eee", false)]
        [DataRow("e?", "e?", true)]
        [TestMethod]
        public void EscapeQuestion(string input, string match, bool shouldSucceed) => RegexPattern(input.EscapePattern(), match, shouldSucceed);

        [DataRow("e*", "ee", true)]
        [DataRow("e*", "eee", true)]
        [DataRow("e*", "fee", false)]
        [DataRow("e*", "e*", true)]
        [TestMethod]
        public void RegularStar(string input, string match, bool shouldSucceed) => RegexPattern(input, match, shouldSucceed);

        [DataRow("e*", "ee", false)]
        [DataRow("e*", "eee", false)]
        [DataRow("e*", "fee", false)]
        [DataRow("e*", "e*", true)]
        [TestMethod]
        public void EscapeStar(string input, string match, bool shouldSucceed) => RegexPattern(input.EscapePattern(), match, shouldSucceed);

        [DataRow("e\\?", "e?", true)]
        [DataRow("e\\?", "e?e", false)]
        [DataRow("e\\?", "ee", false)]
        [DataRow("e\\?", "e\\?", false)]
        [TestMethod]
        public void EscapedQuestion(string input, string match, bool shouldSucceed) => RegexPattern(input, match, shouldSucceed);
      
        [DataRow("e\\?", "e?", false)]
        [DataRow("e\\?", "e?e", false)]
        [DataRow("e\\?", "ee", false)]
        [DataRow("e\\?", "e\\?", true)]
        [TestMethod]
        public void DoubleEscapedQuestion(string input, string match, bool shouldSucceed) => RegexPattern(input.EscapePattern(), match, shouldSucceed);

        [DataRow("e\\*", "e*e", false)]
        [DataRow("e\\*", "ee", false)]
        [DataRow("e\\*", "e*", true)]
        [DataRow("e\\*", "e\\*", false)]
        [TestMethod]
        public void EscapedStar(string input, string match, bool shouldSucceed) => RegexPattern(input, match, shouldSucceed);

        [DataRow("e\\*", "e*e", false)]
        [DataRow("e\\*", "ee", false)]
        [DataRow("e\\*", "e*", false)]
        [DataRow("e\\*", "e\\*", true)]
        [TestMethod]
        public void DoubleEscapedStar(string input, string match, bool shouldSucceed) => RegexPattern(input.EscapePattern(), match, shouldSucceed);

        [DataRow("e\\\\*", "e\\*e", true)]
        [DataRow("e\\\\*", "ee", false)]
        [DataRow("e\\\\*", "e*", false)]
        [DataRow("e\\\\*", "e\\\\*", true)]
        [TestMethod]
        public void EscapedSlash(string input, string match, bool shouldSucceed) => RegexPattern(input, match, shouldSucceed);

        [DataRow("e\\\\*", "e\\*e", false)]
        [DataRow("e\\\\*", "ee", false)]
        [DataRow("e\\\\*", "e*", false)]
        [DataRow("e\\\\*", "e\\\\*", true)]
        [TestMethod]
        public void DoubleEscapedSlash(string input, string match, bool shouldSucceed) => RegexPattern(input.EscapePattern(), match, shouldSucceed);

        [DataRow("e\\\\\\*", "e\\*", true)]
        [DataRow("e\\\\\\*", "e\\?", false)]
        [TestMethod]
        public void EscapedSlashStar(string input, string match, bool shouldSucceed) => RegexPattern(input, match, shouldSucceed);
    }
}