using ACMESharp.Crypto;
using ACMESharp.Crypto.JOSE;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PKISharp.WACS.Clients.Acme
{
    class ExternalAccountBinding
    {
        public string AccountKey { get; set; }
        public string Algorithm { get; set; }
        public string Key { get; set; }
        public string KeyIdentifier { get; set; }
        public string Url { get; set; }

        public ExternalAccountBinding(string algorithm, string accountKey, string keyIdentifier, string key, string url)
        {
            Algorithm = algorithm;
            AccountKey = accountKey;
            KeyIdentifier = keyIdentifier;
            Url = url;
            Key = key;
        }

        public JwsSignedPayload Payload()
        {
            var protectedHeader = new Dictionary<string, object>
            {
                ["alg"] = Algorithm,
                ["kid"] = KeyIdentifier,
                ["url"] = Url
            };
            return JwsHelper.SignFlatJsonAsObject(Sign, AccountKey, protectedHeader, null);
        }

        public byte[] Sign(byte[] input)
        {
            var keyBytes = CryptoHelper.Base64.UrlDecode(Key);
            switch (Algorithm)
            {
                case "HS256":
                    {
                        using var hmac = new HMACSHA256(keyBytes);
                        return hmac.ComputeHash(input);
                    }
                case "HS384":
                    {
                        using var hmac = new HMACSHA384(keyBytes);
                        return hmac.ComputeHash(input);
                    }
                case "HS512":
                    {
                        using var hmac = new HMACSHA512(keyBytes);
                        return hmac.ComputeHash(input);
                    }
            }
            throw new InvalidOperationException();
        }
    }
}
