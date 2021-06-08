using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class SecretService : ISecretService
    {
        private readonly List<Tuple<string, string>> _secrets;

        public SecretService()
        {
            _secrets = new();
            _secrets.Add(new Tuple<string, string>("key1", "secret1"));
            _secrets.Add(new Tuple<string, string>("key2", "secret2"));
        }

        public string Prefix => "mock";
        public void DeleteSecret(string key) => _secrets.RemoveAll(x => x.Item1 == key);
        public string? GetSecret(string? identifier) => _secrets.FirstOrDefault(x => x.Item1 == identifier)?.Item2;
        public IEnumerable<string> ListKeys() => _secrets.Select(x => x.Item1);
        public void PutSecret(string identifier, string secret)
        {
            var existing = _secrets.FirstOrDefault(x => x.Item1 == identifier);
            if (existing != null)
            {
                _ = _secrets.Remove(existing);
            }
            _secrets.Add(new Tuple<string, string>(identifier, secret));
        }

        public void Save() { }
    }
}
