using Microsoft.VisualStudio.TestTools.UnitTesting;
using PKISharp.WACS.Plugins.CsrPlugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using mock = PKISharp.WACS.UnitTests.Mock.Services;
using real = PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Tests.CsrPluginTests
{
    [TestClass]
    public class PostProcessTest
    {
        [TestMethod]
        public void Convert()
        {
            var log = new mock.LogService(false);
            var settings = new mock.MockSettingsService();
            var pem = new real.PemService();
            var input = new mock.InputService(new List<string>());
            var x = new Rsa(log, settings,  pem, new RsaOptions());
            var data = Assembly.
                GetAssembly(typeof(PostProcessTest))!.
                GetManifestResourceStream("PKISharp.WACS.UnitTests.Tests.CsrPluginTests.original.pfx");
            if (data == null)
            {
                throw new Exception();
            }
            var ms = new MemoryStream();
            data.CopyTo(ms);
            var bytes = ms.ToArray();
            var tempFile = Path.GetTempFileName();
            File.WriteAllBytes(tempFile, bytes);
            var fi = new FileInfo(tempFile);
            var certInfo = real.CertificateService.GetInfo(fi, "A8<TEpyPweWMO1m(");
            _ = x.PostProcess(certInfo.Certificate).Result;
            Assert.IsTrue(true);
        }
    }
}
