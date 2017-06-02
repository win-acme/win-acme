using System;
using letsencrypt.Support;
using Microsoft.Web.Administration;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using Moq;
using System.Linq;

namespace letsencrypt_tests.Support
{
    internal class MockIISServerManager : IIISServerManager
    {
        private IEnumerable<IIISSite> FakeSites
        {
            get
            {
                var mockSite = new Mock<IIISSite>();
                mockSite.Setup((m) => m.Id).Returns(0);
                mockSite.Setup((m) => m.Bindings).Returns(FakeBindings);

                return new[] { mockSite.Object };
            }
        }

        private IEnumerable<IIISBinding> FakeBindings
        {
            get
            {
                var binding = new Mock<IIISBinding>();
                binding.Setup(m => m.Host).Returns("localhost");
                binding.Setup(m => m.Protocol).Returns("http");

                return new List<IIISBinding> { binding.Object };
            }
        }

        private ApplicationCollection FakeApplications
        {
            get
            {
                var application = new Mock<Application>();
                application.Setup(m => m.VirtualDirectories).Returns(FakeVirtualDirectories);

                IEnumerable<Application> items = new List<Application> { application.Object };
                var mock = new Mock<ApplicationCollection>();
                mock.Setup(m => m.Count).Returns(items.Count);
                mock.Setup(m => m[It.IsAny<int>()]).Returns<int>(i => items.ElementAt(i));
                mock.Setup(m => m.GetEnumerator()).Returns(() => items.GetEnumerator());
                
                return mock.Object;
            }
        }

        private VirtualDirectoryCollection FakeVirtualDirectories
        {
            get
            {
                var localPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var mockVD = new Mock<VirtualDirectory>();
                mockVD.Setup(m => m.PhysicalPath).Returns(localPath);

                IEnumerable<VirtualDirectory> items = new List<VirtualDirectory> { mockVD.Object };
                var mock = new Mock<VirtualDirectoryCollection>();
                mock.Setup(m => m.Count).Returns(items.Count);
                mock.Setup(m => m[It.IsAny<int>()]).Returns<int>(i => items.ElementAt(i));
                mock.Setup(m => m.GetEnumerator()).Returns(() => items.GetEnumerator());

                return mock.Object;
            }
        }

        public IEnumerable<IIISSite> Sites
        {
            get
            {
                return FakeSites;
            }
        }

        public void CommitChanges()
        {
        }

        public void Dispose()
        {
        }

        public Version GetVersion()
        {
            return new Version(8, 0);
        }
    }
}