using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class SecretService : ISecretService
    {
        public string Prefix => throw new System.NotImplementedException();
        public void DeleteSecret(string key) => throw new System.NotImplementedException();
        public string? GetSecret(string? identifier) => null;
        public IEnumerable<string> ListKeys() => throw new System.NotImplementedException();
        public void PutSecret(string identifier, string secret) { }
        public void Save() => throw new System.NotImplementedException();
    }
}
