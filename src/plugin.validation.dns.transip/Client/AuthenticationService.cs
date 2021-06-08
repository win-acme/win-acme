using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TransIp.Library
{
    public class AuthenticationService : BaseService
    {
        private readonly string _login;
        private readonly ICipherParameters? _key;

        public AuthenticationService(string? login, string? privateKey, IProxyService proxyService) : base(proxyService)
        {
            if (string.IsNullOrWhiteSpace(login))
            {
                throw new Exception("No login specified");
            }
            _login = login;
            try
            {
                _key = ParseKey(privateKey);
            } 
            catch (Exception ex)
            {
                throw new Exception("Unable to parse private key", ex);
            }
            if (_key == null)
            {
                throw new Exception("Unable to parse private key");
            }
        }

        protected internal override async Task<HttpClient> GetClient()
        {
            var client = await base.GetClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetToken());
            return client;
        }

        private async Task<string?> GetToken()
        {
            var request = new AuthenticationRequest()
            {
                Login = _login,
                Nonce = Guid.NewGuid().ToString().Substring(0, 32),
                ExpirationTime = "30 minutes",
                GlobalKey = true,
                ReadOnly = false
            };
            var body = JsonConvert.SerializeObject(request);
            var content = new StringContent(body);
            content.Headers.Add("Signature", Sign(body));
            var client = await base.GetClient();
            var response = await client.PostAsync("auth", content);
            var result = await ParseResponse(response);
            var typedResult = new TransIpResponse<AuthenticationResponse>(result);
            return typedResult.PayloadTyped?.Token;
        }

        private string Sign(string body)
        {
            var digest = Digest(body);
            var encrypted = Encrypt(digest);
            return Convert.ToBase64String(encrypted);
        }

        private byte[] Digest(string body)
        {
            var prefix = new[]
			{
				0x30, 0x51, 0x30, 0x0d, 0x06, 0x09,
				0x60, 0x86, 0x48, 0x01, 0x65, 0x03,
				0x04, 0x02, 0x03, 0x05, 0x00, 0x04, 0x40
			};
			var hashAlg = SHA512.Create();
			var hash = hashAlg.ComputeHash(Encoding.ASCII.GetBytes(body));
			return prefix.Select(x => (byte)x).Concat(hash).ToArray();
        }

        private byte[] Encrypt(byte[] digest)
		{
			var cipher = CipherUtilities.GetCipher("RSA/None/PKCS1Padding");
			cipher.Init(true, _key);
			return cipher.DoFinal(digest);
		}

        public static ICipherParameters? ParseKey(string? key)
		{
            if (string.IsNullOrWhiteSpace(key)) {
                return null;
            }
            if (!key.Contains("\n"))
            {
                var innerKey = Regex.Match(key, "(-----.+-----)(.+)?(-----.+-----)", RegexOptions.Multiline);
                if (innerKey.Success)
                {
                    key = innerKey.Groups[1].Value + innerKey.Groups[2].Value.Replace(" ", "\n") + innerKey.Groups[3].Value;
                }
            }
			var keyReader = new StringReader(key);
			var pemReader = new PemReader(keyReader);
            var pemObject = pemReader.ReadObject();
            if (pemObject == null)
            {
                return null;
            }
			var cipherParameters = default(ICipherParameters);
			switch (pemObject) {
				case RsaPrivateCrtKeyParameters parameters:
					cipherParameters = parameters;
					break;
				case AsymmetricCipherKeyPair _:
					var keyPair = (AsymmetricCipherKeyPair) pemObject;
					cipherParameters = keyPair.Private;
					break;
			}
            return cipherParameters;
		}

        private class AuthenticationResponse
        {
            [JsonProperty("token")]
            public string? Token { get; set; }
        }

        private class AuthenticationRequest
        {
            [JsonProperty("nonce")]
            public string? Nonce { get; set; }

            [JsonProperty("login")]
            public string? Login { get; set; }

            [JsonProperty("read_only")]
            public bool ReadOnly { get; set; }

            [JsonProperty("expiration_time")]
            public string? ExpirationTime { get; set; }
            
            [JsonProperty("label")]
            public string? Label { get; set; }
            
            [JsonProperty("global_key")]
            public bool GlobalKey { get; set; }
        }
    }
}
