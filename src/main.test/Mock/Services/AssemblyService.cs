using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class MockAssemblyService : AssemblyService
    {
        public MockAssemblyService(ILogService log) : base(log) 
        { 
            
        }
        public override List<TypeDescriptor> GetResolvable<T>()
        {
            if (typeof(T) == typeof(ISecretService))
            {
                return new List<TypeDescriptor>() { new TypeDescriptor(typeof(SecretService)) };
            }
            return base.GetResolvable<T>();

        }
    }
}
