using ACMESharp.Protocol;
using Newtonsoft.Json;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.IO;
using System.Security.Cryptography;

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
        private AccountSigner? _currentSigner;
        private AccountDetails? _currentAccount;

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
        public AccountSigner NewSigner(string keyType = "ES256")
        {
            _log.Debug("Creating new {keyType} signer", keyType);
            return new AccountSigner(keyType);
        }

        /// <summary>
        /// Create a new default signer
        /// </summary>
        /// <returns></returns>
        public AccountSigner DefaultSigner()
        {
            try
            {
                return NewSigner("ES256");
            }
            catch (CryptographicException cex)
            {
                _log.Verbose("First chance error generating signer: {cex}", cex.Message);
                return NewSigner("RS256");
            }
        }

        /// <summary>
        /// Get or set the currently stored signer
        /// </summary>
        public AccountSigner? CurrentSigner
        {
            get
            {
                if (_currentSigner == null)
                {
                    if (File.Exists(SignerPath))
                    {
                        try
                        {
                            _log.Debug("Loading signer from {signerPath}", SignerPath);
                            var signerString = new ProtectedString(File.ReadAllText(SignerPath), _log);
                            if (signerString.Value != null)
                            {
                                _currentSigner = JsonConvert.DeserializeObject<AccountSigner>(signerString.Value);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Unable to load signer");
                        }
                    } 
                    else
                    {
                        _log.Debug("No signer found at {signerPath}", SignerPath);
                    }
                }
                return _currentSigner;
            }
            set
            {
                if (value != null)
                {
                    _log.Debug("Saving signer to {SignerPath}", SignerPath);
                    var x = new ProtectedString(JsonConvert.SerializeObject(value));
                    File.WriteAllText(SignerPath, x.DiskValue(_settings.Security.EncryptConfig));
                } 
                _currentSigner = value;
            }
        }

        /// <summary>
        /// Get or set the currently stored account
        /// </summary>
        public AccountDetails? CurrentAccount
        {
            get
            {
                if (_currentAccount == null)
                {
                    if (File.Exists(AccountPath))
                    {
                        if (CurrentSigner != null)
                        {
                            _log.Debug("Loading account from {accountPath}", AccountPath);
                            _currentAccount = JsonConvert.DeserializeObject<AccountDetails>(File.ReadAllText(AccountPath));
                            // Maybe we should update the account details 
                            // on every start of the program to figure out
                            // if it hasn't been suspended or cancelled?
                            // UpdateAccount();
                        }
                        else
                        {
                            _log.Error("Account found but no valid signer could be loaded");
                        }
                    } 
                    else
                    {
                        _log.Debug("No account found at {accountPath}", AccountPath);
                    }
                }
                return _currentAccount;
            }
            set
            {
                if (value != null)
                {
                    _log.Debug("Saving account to {AccountPath}", AccountPath);
                    File.WriteAllText(AccountPath, JsonConvert.SerializeObject(value));
                }
                _currentAccount = value;
            }
        }

        /// <summary>
        /// Encrypt/decrypt signer information
        /// </summary>
        internal void Encrypt()
        {
            try
            {
                var signer = CurrentSigner;
                CurrentSigner = signer; //forces a re-save of the signer
                _log.Information("Signer re-saved");
            }
            catch
            {
                _log.Error("Cannot re-save signer as it is likely encrypted on a different machine");
            }
        }
    }
}
