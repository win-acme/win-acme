using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockAssemblyService : AssemblyService
    {
        public MockAssemblyService(ILogService log) : base(log) { }
    }
}
