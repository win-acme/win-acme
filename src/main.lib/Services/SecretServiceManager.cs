using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class SecretServiceManager
    {
        private readonly ISecretService _secretService;
        private readonly IInputService _inputService;
        public const string VaultPrefix = "vault://";

        public SecretServiceManager(ISecretService secretService, IInputService input) {
            _secretService = secretService;
            _inputService = input;
        }

        /// <summary>
        /// Get a secret from interactive mode setup
        /// </summary>
        /// <param name="purpose"></param>
        /// <returns></returns>
        public async Task<string?> GetSecret(string purpose, string? secret = null)
        {
            var stop = false;
            while (!stop && string.IsNullOrWhiteSpace(secret))
            {
                var options = new List<Choice<Func<Task<string?>>>>
                {
                    Choice.Create<Func<Task<string?>>>(
                        () => { stop = true; return Task.FromResult<string?>(null); },
                        description: "Skip this step"),
                    Choice.Create<Func<Task<string?>>>(
                        () => _inputService.ReadPassword(purpose),
                        description: "Type or paste in console"),
                     Choice.Create<Func<Task<string?>>>(
                        () => FindSecret(),
                        description: "Search in vault"),
                };
                var chosen = await _inputService.ChooseFromMenu($"How would you like to provide the {purpose}?", options);
                secret = await chosen.Invoke();
            }

            if (stop)
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(secret))
            {
                return secret;
            }

            // Offer to save in list
            if (!secret.StartsWith(VaultPrefix))
            {
                var save = await _inputService.PromptYesNo($"Save {purpose} to vault for future reference?", false);
                if (save)
                {
                    return await ChooseKeyAndStoreSecret(secret);
                }
            }
            return secret;
        }

        /// <summary>
        /// Add a secret to the store from the main menu
        /// </summary>
        /// <returns></returns>
        public async Task<string?> AddSecret()
        {
            var secret = await _inputService.ReadPassword("Secret");
            if (!string.IsNullOrWhiteSpace(secret))
            {
                return await ChooseKeyAndStoreSecret(secret);
            } 
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Pick key and store the secret in the vault
        /// </summary>
        /// <param name="secret"></param>
        /// <returns></returns>
        private async Task<string> ChooseKeyAndStoreSecret(string secret)
        {
            var key = "";
            while (string.IsNullOrEmpty(key))
            {
                key = await _inputService.RequestString("Please provide a unique name to reference this secret", false);
                key = key.Trim().ToLower().Replace(" ", "-");
                if (_secretService.ListKeys().Contains(key))
                {
                    var overwrite = await _inputService.PromptYesNo($"Key {key} already exists in vault, overwrite?", true);
                    if (!overwrite)
                    {
                        key = null;
                    }
                }
            }
            _secretService.PutSecret(key, secret);
            return $"{VaultPrefix}{_secretService.Prefix}/{key}";
        }

        /// <summary>
        /// List secrets currently in vault as choices to pick from
        /// </summary>
        /// <returns></returns>
        private async Task<string?> FindSecret()
        {
            var chosenKey = await _inputService.ChooseOptional(
                "Which vault secret do you want to use?",
                _secretService.ListKeys(),
                (key) => Choice.Create<string?>(key), 
                "Cancel");
            if (chosenKey == null)
            {
                return null;
            }
            else
            {
                return $"{VaultPrefix}{_secretService.Prefix}/{chosenKey}";
            }
        }

        /// <summary>
        /// Shortcut method
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string? EvaluateSecret(ProtectedString? input) => EvaluateSecret(input?.Value);

        /// <summary>
        /// Try to interpret the secret input as a vault reference
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string? EvaluateSecret(string? input)
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