using ACMESharp.Protocol;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace PKISharp.WACS.Clients.Acme
{
    /// <summary>
    /// Manage the account used by the AcmeClient
    /// </summary>
    class AccountManager
    {
        private const string SignerFileName = "Signer_v2";
        private const string RegistrationFileName = "Registration_v2";

        private readonly ILogService _log;
        private readonly ISettingsService _settings;

        public AccountManager(
            ILogService log,
            ISettingsService settings)
        {
            _log = log;
            _settings = settings;
        }

        /// <summary>
        /// File that contains information about the signer, which
        /// cryptographically signs the messages sent to the ACME 
        /// server so that the account can be authenticated
        /// </summary>
        private string SignerPath => Path.Combine(_settings.Client.ConfigurationPath, SignerFileName);

        /// <summary>
        /// File that contains information about the account
        /// </summary>
        private string AccountPath => Path.Combine(_settings.Client.ConfigurationPath, RegistrationFileName);

        /// <summary>
        /// Create a new signer using the specified algorithm
        /// </summary>
        /// <param name="keyType"></param>
        /// <returns></returns>
        private AccountSigner NewSigner(string keyType)
        {
            _log.Debug("Creating new {keyType} signer", keyType);
            return new AccountSigner(keyType);
        }

        /// <summary>
        /// Create a new default signer
        /// </summary>
        /// <returns></returns>
        public Account NewAccount(string keyType = "ES256")
        {
            AccountSigner? signer;
            try
            {
                signer = NewSigner(keyType);
            }
            catch (CryptographicException cex)
            {
                if (keyType == "ES256")
                {
                    _log.Verbose("First chance error generating signer: {cex}", cex.Message);
                    signer = NewSigner("RS256");
                } 
                else
                {
                    throw;
                }
            }
            return new Account(default, signer);
        }

        /// <summary>
        /// Load the default account for the endpoint
        /// </summary>
        public Account? DefaultAccount
        {
            get
            {
                var details = DefaultAccountDetails;
                var signer = DefaultAccountSigner;
                if (details != default && signer != null)
                {
                    return new Account(details, signer);
                }
                return null;
            }
            set
            {
                DefaultAccountSigner = value?.Signer;
                DefaultAccountDetails = value?.Details ?? default;
            }
        }

        /// <summary>
        /// Get or set the currently stored signer
        /// </summary>
        private AccountSigner? DefaultAccountSigner
        {
            get
            {
                if (File.Exists(SignerPath))
                {
                    try
                    {
                        _log.Debug("Loading signer from {signerPath}", SignerPath);
                        var signerString = new ProtectedString(File.ReadAllText(SignerPath), _log);
                        if (signerString.Value != null)
                        {
                            return JsonSerializer.Deserialize(signerString.Value, AcmeClientJson.Insensitive.AccountSigner);
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Unable to load signer");
                    }
                }
                return null;
            }
            set
            {
                if (value != null)
                {
                    _log.Debug("Saving signer to {SignerPath}", SignerPath);
                    var x = new ProtectedString(JsonSerializer.Serialize(value, AcmeClientJson.Default.AccountSigner));
                    File.WriteAllText(SignerPath, x.DiskValue(_settings.Security.EncryptConfig));
                }
            }
        }

        /// <summary>
        /// Get or set the currently stored account
        /// </summary>
        private AccountDetails DefaultAccountDetails
        {
            get
            {
                if (File.Exists(AccountPath))
                {
                    _log.Debug("Loading account from {accountPath}", AccountPath);
                    return JsonSerializer.Deserialize(File.ReadAllText(AccountPath), AcmeClientJson.Insensitive.AccountDetails);
                }
                return default;
            }
            set
            {
                if (value != default)
                {
                    _log.Debug("Saving account to {AccountPath}", AccountPath);
                    File.WriteAllText(AccountPath, JsonSerializer.Serialize(value, AcmeClientJson.Insensitive.AccountDetails));
                }
            }
        }

        /// <summary>
        /// Encrypt/decrypt signer information
        /// </summary>
        internal void Encrypt()
        {
            try
            {
                var signer = DefaultAccountSigner;
                DefaultAccountSigner = signer; //forces a re-save of the signer
                _log.Information("Signer re-saved");
            }
            catch
            {
                _log.Error("Cannot re-save signer as it is likely encrypted on a different machine");
            }
        }
    }
}
