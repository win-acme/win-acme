using ACMESharp.Crypto.JOSE;
using ACMESharp.Crypto.JOSE.Impl;
using ACMESharp.Protocol;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Clients.Acme
{
    [JsonSerializable(typeof(AccountSigner))]
    [JsonSerializable(typeof(AccountDetails))]
    internal partial class AcmeClientJson : JsonSerializerContext {
        public static AcmeClientJson Insensitive { get; } = new AcmeClientJson(new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
    }

    /// <summary>
    /// This is "password" for the ACME account, it can be 
    /// an RSA or Elliptic curve private key
    /// </summary>
    internal class AccountSigner
    {
        private string? _keyType;
        private string? _keyExport;
        private IJwsTool? _jwsTool;

        public AccountSigner() { }
        public AccountSigner(string keyType)
        {
            KeyType = keyType;
            KeyExport = JwsTool().Export();
        }
        public AccountSigner(IJwsTool source)
        {
            KeyType = source.JwsAlg;
            KeyExport = source.Export();
        }

        /// <summary>
        /// Type of signature algorithm, default ES256
        /// </summary>
        public string? KeyType 
        {
            get => _keyType;
            set { _keyType = value; _jwsTool = null; }
        }

        /// <summary>
        /// Public/private key data for persistance
        /// </summary>
        public string? KeyExport
        {
            get => _keyExport;
            set { _keyExport = value; _jwsTool = null; }
        }

        public IJwsTool JwsTool()
        {
            if (_jwsTool != null)
            {
                return _jwsTool;
            }

            if (string.IsNullOrWhiteSpace(KeyType))
            {
                throw new Exception($"Missing KeyType");
            }
            IJwsTool? ret = null;
            if (KeyType.StartsWith("ES"))
            {
                ret = new ESJwsTool
                {
                    HashSize = int.Parse(KeyType.Substring(2))
                };
            }
            else if (KeyType.StartsWith("RS"))
            {
                ret = new RSJwsTool();
            }
            if (ret == null)
            {
                throw new Exception($"Unknown or unsupported KeyType [{KeyType}]");
            }

            // Initialize
            ret.Init();
            if (!string.IsNullOrEmpty(KeyExport))
            {
                ret.Import(KeyExport);
            }

            // Save for future reference
            _jwsTool = ret;
            return _jwsTool;
        }
    }
}