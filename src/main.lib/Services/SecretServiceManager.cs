using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Services
{
    public class SecretServiceManager
    {
        private readonly ISecretService _secretService;
        public const string VaultPrefix = "vault://";

        public SecretServiceManager(ISecretService secretService) => _secretService = secretService;

        public string? GetSecret(ProtectedString? input) => GetSecret(input?.Value);

        public string? GetSecret(string? input)
        {
            if (input == null)
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }
            if (input.StartsWith(VaultPrefix))
            {
                var remainingValue = input[VaultPrefix.Length..];
                var providerKey = $"{_secretService.Prefix}/";
                if (remainingValue.StartsWith(providerKey))
                {
                    var key = remainingValue[providerKey.Length..];
                    return _secretService.GetSecret(key);
                }
            }
            return input;
        }
    }
}