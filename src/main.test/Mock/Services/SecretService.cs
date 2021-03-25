using PKISharp.WACS.Services;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class SecretService : ISecretService
    {
        public string Prefix => throw new System.NotImplementedException();
        public string? GetSecret(string? identifier) => null;
        public void PutSecret(string identifier, string secret) { }
        public void Save() => throw new System.NotImplementedException();
    }
}
