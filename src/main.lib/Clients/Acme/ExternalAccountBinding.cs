using ACMESharp;
using ACMESharp.Crypto;
using ACMESharp.Crypto.JOSE;
using System;
using System.Security.Cryptography;
using System.Text.Json;
using static ACMESharp.Crypto.JOSE.JwsHelper;

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
            var ph = new ProtectedHeader
            {
                Algorithm = Algorithm,
                KeyIdentifier = KeyIdentifier,
                Url = Url
            };
            var protectedHeader = JsonSerializer.Serialize(ph, AcmeJson.Insensitive.ProtectedHeader);
            return SignFlatJsonAsObject(Sign, AccountKey, protectedHeader);
        }

        public byte[] Sign(byte[] input)
        {
            var keyBytes = Base64Tool.UrlDecode(Key);
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
            throw new InvalidOperationException($"Unsupported algorithm {Algorithm}");
        }
    }
}
