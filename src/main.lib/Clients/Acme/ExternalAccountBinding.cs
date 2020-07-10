using ACMESharp.Crypto;
using ACMESharp.Crypto.JOSE;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace PKISharp.WACS.Clients.Acme
{
    class ExternalAccountBinding
    {
        public string AccountKey { get; set; }
        public string Key { get; set; }
        public string KeyIdentifier { get; set; }
        public string Url { get; set; }

        public ExternalAccountBinding(string accountKey, string keyIdentifier, string key, string url)
        {
            AccountKey = accountKey;
            KeyIdentifier = keyIdentifier;
            Url = url;
            Key = key;
        }

        public JwsSignedPayload Payload()
        {
            var protectedHeader = new Dictionary<string, object>
            {
                ["alg"] = "HS256",
                ["kid"] = KeyIdentifier,
                ["url"] = Url
            };
            return JwsHelper.SignFlatJsonAsObject(Sign, AccountKey, protectedHeader, null);
        }

        public byte[] Sign(byte[] input)
        {
            var keyBytes = CryptoHelper.Base64.UrlDecode(Key);
            using var hmac = new HMACSHA256(keyBytes);
            return hmac.ComputeHash(input);
        }
    }
}
