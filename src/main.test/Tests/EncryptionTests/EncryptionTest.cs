using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.UnitTests.Tests.EcnryptionTests
{
    [TestClass]
    public class EncryptionTests
    {
        [TestMethod]
        public void TurnOnOff()
        {
            // Create encrypted value
            var plain = "---BLA---";
            var plainString = new ProtectedString(plain);
            var encrypted = plainString.DiskValue(true);
            Assert.IsTrue(encrypted != null);

            // Read back
            var log = new Mock.Services.LogService(false);
            var readBack = new ProtectedString(encrypted ?? "", log);
            Assert.AreEqual(plain, readBack.Value);

            // Turn off encryption
            var turnOff = new ProtectedString(encrypted ?? "", log);
            var turnOffValue = turnOff.DiskValue(false);
            Assert.IsTrue(turnOffValue != null);

            // Read back turned off value
            var readBack2 = new ProtectedString(turnOffValue ?? "", log);
            Assert.AreEqual(readBack2.Value, plain);
        }
    }
}
