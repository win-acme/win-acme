using ACMESharp.Protocol;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
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
        internal Account NewAccount(string keyType = "ES256")
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
        /// Load named account
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal Account? LoadAccount(string? name = null)
        {
            var signerPath = GetPath(SignerFileName, name);
            var detailsPath = GetPath(RegistrationFileName, name);
            var signer = LoadSigner(signerPath);
            var details = LoadDetails(detailsPath);
            if (details == default)
            {
                return null;
            }
            if (signer == null)
            {
                return null;
            }
            return new Account(details, signer);
        }

        /// <summary>
        /// Store named account
        /// </summary>
        /// <param name="account"></param>
        /// <param name="name"></param>
        internal void StoreAccount(Account account, string? name = null)
        {
            var signerPath = GetPath(SignerFileName, name);
            var detailsPath = GetPath(RegistrationFileName, name);
            StoreDetails(account.Details, detailsPath);
            StoreSigner(account.Signer, signerPath);
        }

        /// <summary>
        /// Optional prefix for the path
        /// </summary>
        /// <param name="file"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetPath(string file, string? name = null) 
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                name = name.CleanPath();
                file = $"{name}.{file}";
            }
            return Path.Combine(_settings.Client.ConfigurationPath, file);
        }

        /// <summary>
        /// Load signer from disk
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private AccountSigner? LoadSigner(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    _log.Debug("Loading signer from {signerPath}", path);
                    var signerString = new ProtectedString(File.ReadAllText(path), _log);
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
            else
            {
                _log.Debug("Signer not found at {signerPath}", path);
            }
            return null;
        }

        /// <summary>
        /// Store signer to disk
        /// </summary>
        /// <param name="signer"></param>
        /// <param name="path"></param>
        private void StoreSigner(AccountSigner? signer, string path)
        {
            if (signer != null)
            {
                _log.Debug("Saving signer to {SignerPath}", path);
                var x = new ProtectedString(JsonSerializer.Serialize(signer, AcmeClientJson.Default.AccountSigner));
                File.WriteAllText(path, x.DiskValue(_settings.Security.EncryptConfig));
            }
        }

        /// <summary>
        /// Load details from disk
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private AccountDetails LoadDetails(string path)
        {
            if (File.Exists(path))
            {
                _log.Debug("Loading account from {path}", path);
                return JsonSerializer.Deserialize(File.ReadAllText(path), AcmeClientJson.Insensitive.AccountDetails);
            }
            else
            {
                _log.Debug("Details not found at {path}", path);
            }
            return default;
        }

        /// <summary>
        /// Store details to disk
        /// </summary>
        /// <param name="details"></param>
        /// <param name="path"></param>
        private void StoreDetails(AccountDetails details, string path)
        {
            if (details != default)
            {
                _log.Debug("Saving account to {AccountPath}", path);
                File.WriteAllText(path, JsonSerializer.Serialize(details, AcmeClientJson.Insensitive.AccountDetails));
            }
        }

        /// <summary>
        /// List of available accounts
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<string> ListAccounts()
        {
            var dir = new DirectoryInfo(_settings.Client.ConfigurationPath);
            foreach (var account in dir.EnumerateFiles($"*{RegistrationFileName}"))
            {
                yield return account.Name.Replace(RegistrationFileName, "").TrimEnd('.');
            }
        }

        /// <summary>
        /// Encrypt/decrypt signer information
        /// </summary>
        internal void Encrypt()
        {
            try
            {
                foreach (var name in ListAccounts())
                {
                    var account = LoadAccount(name);
                    if (account != null)
                    {
                        StoreAccount(account, name); //forces a re-save of the signer
                    } 
                    else
                    {
                        _log.Error($"Unable to load account {name}");
                    }
                }
                _log.Information("Signer re-saved");
            }
            catch
            {
                _log.Error("Cannot re-save account (created on a different machine?)");
            }
        }
    }
}
