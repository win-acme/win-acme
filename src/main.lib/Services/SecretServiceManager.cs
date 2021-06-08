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
        private readonly ILogService _logService;
        public const string VaultPrefix = "vault://";

        public SecretServiceManager(
            ISecretService secretService,
            IInputService input, 
            ILogService logService) 
        {
            _secretService = secretService;
            _inputService = input;
            _logService = logService;
        }

        /// <summary>
        /// Get a secret from interactive mode setup
        /// </summary>
        /// <param name="purpose"></param>
        /// <returns></returns>
        public async Task<string?> GetSecret(string purpose, string? @default = null, string? none = null, bool required = false, bool multiline = false)
        {
            var stop = false;
            string? ret = null;
            // While loop allows the "Find in vault" option
            // to be cancelled so that the user can still
            // input a new password if it's not found yet
            // without having to restart the process.
            while (!stop && string.IsNullOrEmpty(ret))
            {
                var options = new List<Choice<Func<Task<string?>>>>();
                if (!required)
                {
                    options.Add(Choice.Create<Func<Task<string?>>>(
                        () => {
                            stop = true;
                            return Task.FromResult(none);
                        },
                        description: "None"));
                }
                options.Add(Choice.Create<Func<Task<string?>>>(
                    async () => {
                        stop = true;
                        if (multiline)
                        {
                            return await _inputService.RequestString(purpose, true);
                        }
                        else
                        {
                            return await _inputService.ReadPassword(purpose);
                        }
                    },
                    description: "Type/paste in console"));
                options.Add(Choice.Create<Func<Task<string?>>>(
                        () => FindSecret(),
                        description: "Search in vault"));
                if (!string.IsNullOrWhiteSpace(@default))
                {
                    options.Add(Choice.Create<Func<Task<string?>>>(
                        () => { 
                            stop = true; 
                            return Task.FromResult<string?>(@default); 
                        },
                        description: "Default"));
                }

                // Handle undefined input as direct password
                Choice<Func<Task<string?>>> processUnkown(string? unknown) => Choice.Create<Func<Task<string?>>>(() => Task.FromResult(unknown));

                var chosen = await _inputService.ChooseFromMenu("Choose from the menu", options, (x) => processUnkown(x));
                ret = await chosen.Invoke();
            }

            if (ret == none)
            {
                return none;
            }
            if (ret == @default || ret == null)
            {
                return @default;
            }

            // Offer to save in list
            if (!ret.StartsWith(VaultPrefix))
            {
                var save = await _inputService.PromptYesNo($"Save to vault for future reuse?", false);
                if (save)
                {
                    return await ChooseKeyAndStoreSecret(ret);
                }
            }
            return ret;
        }

        /// <summary>
        /// Add a secret to the store from the main menu
        /// </summary>
        /// <returns></returns>
        public void Encrypt() => _secretService.Save();

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
            return FormatKey(key);
        }

        /// <summary>
        /// Format the key
        /// </summary>
        /// <returns></returns>
        public string FormatKey(string key) => $"{VaultPrefix}{_secretService.Prefix}/{key}";

        /// <summary>
        /// List secrets currently in vault as choices to pick from
        /// </summary>
        /// <returns></returns>
        private async Task<string?> FindSecret()
        {
            var chosenKey = await _inputService.ChooseOptional(
                "Which vault secret do you want to use?",
                _secretService.ListKeys(),
                (key) => Choice.Create<string?>(key, description: FormatKey(key)),
                "Cancel");
            if (chosenKey == null)
            {
                return null;
            }
            else
            {
                return FormatKey(chosenKey);
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

        /// <summary>
        /// Manage secrets from the main menu
        /// </summary>
        /// <returns></returns>
        internal async Task ManageSecrets()
        {
            var exit = false;
            while (!exit)
            {
                var choices = _secretService.ListKeys().Select(x => Choice.Create<Func<Task>>(() => EditSecret(x), description: FormatKey(x))).ToList();
                choices.Add(Choice.Create<Func<Task>>(() => AddSecret(), "Add secret", command: "A"));
                choices.Add(Choice.Create<Func<Task>>(() => { 
                    exit = true; 
                    return Task.CompletedTask; 
                }, "Back to main menu", command: "Q", @default: true));
                var chosen = await _inputService.ChooseFromMenu("Choose an existing secret to manage, add a new one", choices);
                await chosen.Invoke();
            }

        }

        /// <summary>
        /// Edit or delete existing secret
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal async Task EditSecret(string key) {
            var exit = false;
            while (!exit)
            {
                var secret = _secretService.GetSecret(key);
                _inputService.CreateSpace();
                _inputService.Show("Reference", key);
                _inputService.Show("Secret", "********");
                var choices = new List<Choice<Func<Task>>>
                {
                    Choice.Create<Func<Task>>(() => ShowSecret(key), "Show secret", command: "S"),
                    Choice.Create<Func<Task>>(() => UpdateSecret(key), "Update secret", command: "U"),
                    Choice.Create<Func<Task>>(() => { 
                        exit = true; 
                        return DeleteSecret(key);
                    }, "Delete secret", command: "D")
                };
                choices.Add(Choice.Create<Func<Task>>(() => { 
                    exit = true; 
                    return Task.CompletedTask; 
                }, "Back to list", command: "Q", @default: true));
                var chosen = await _inputService.ChooseFromMenu("Choose an option", choices);
                await chosen.Invoke();
            }
        }

        /// <summary>
        /// Delete a secret from the store
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private Task DeleteSecret(string key)
        {
            _secretService.DeleteSecret(key);
            _logService.Warning($"Secret {key} deleted from {_secretService.Prefix} store");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Update a secret in the store
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private async Task UpdateSecret(string key)
        {
            var secret = await _inputService.ReadPassword("Secret");
            if (!string.IsNullOrWhiteSpace(secret))
            {
                _secretService.PutSecret(key, secret);
            }
            else
            {
                _logService.Warning("No input provided, update cancelled");
            }
        }

        /// <summary>
        /// Show secret on screen
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private Task ShowSecret(string key) {
            var secret = _secretService.GetSecret(key);
            _inputService.Show("Secret", secret);
            return Task.CompletedTask;
        }
    }
}