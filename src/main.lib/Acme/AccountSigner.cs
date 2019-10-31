using ACMESharp.Crypto.JOSE;
using ACMESharp.Crypto.JOSE.Impl;
using System;

namespace PKISharp.WACS.Acme
{
    internal class AccountSigner
    {
        public string KeyType { get; set; }
        public string KeyExport { get; set; }

        public IJwsTool JwsTool()
        {
            if (KeyType.StartsWith("ES"))
            {
                var tool = new ESJwsTool
                {
                    HashSize = int.Parse(KeyType.Substring(2))
                };
                tool.Init();
                tool.Import(KeyExport);
                return tool;
            }

            if (KeyType.StartsWith("RS"))
            {
                var tool = new RSJwsTool();
                tool.Init();
                tool.Import(KeyExport);
                return tool;
            }

            throw new Exception($"Unknown or unsupported KeyType [{KeyType}]");
        }
    }
}