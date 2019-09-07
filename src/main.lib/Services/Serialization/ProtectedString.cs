using System;
using System.Security.Cryptography;
using System.Text;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Wrapper to handle string encryption and encoding
    /// Strings can be in three forms:
    /// - Clear, prefixed by ClearPrefix
    /// - Base64 encoded, without any prefix
    /// - Base64 encoded *with* encryption, prefixed by EncryptedPrefix
    /// </summary>
    public class ProtectedString
    {

        /// <summary>
        /// Indicates encryption
        /// </summary>
        internal const string EncryptedPrefix = "enc-";

        /// <summary>
        /// Indicates clear text
        /// </summary>
        internal const string ClearPrefix = "clear-";

        /// <summary>
        /// Logging service, used only by the JsonConverter
        /// </summary>
        private readonly ILogService _log;

        /// <summary>
        /// Indicates if there was an error decoding or decrypting the string
        /// </summary>
        public bool Error { get; private set; } = false;

        /// <summary>
        /// Clear value, should be used for operations
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Value to save to disk, based on the setting
        /// </summary>
        public string DiskValue(bool encrypt) => encrypt ? ProtectedValue : EncodedValue;

        /// <summary>
        /// Constructor for user input, always starting with clear text
        /// </summary>
        /// <param name="clearValue"></param>
        public ProtectedString(string clearValue) => Value = clearValue;

        /// <summary>
        /// Constructor for deserialisation, may be any format
        /// </summary>
        /// <param name="rawValue"></param>
        /// <param name="log"></param>
        public ProtectedString(string rawValue, ILogService log)
        {
            _log = log;
            Value = rawValue;

            if (!string.IsNullOrEmpty(rawValue))
            {
                if (rawValue.StartsWith(EncryptedPrefix))
                {
                    // Sure to be encrypted
                    try
                    {
                        Value = Unprotect(rawValue.Substring(EncryptedPrefix.Length));
                    }
                    catch
                    {
                        _log.Error("Unable to decrypt configuration value, may have been written by a different machine.");
                        Error = true;
                    }
                }
                else if (rawValue.StartsWith(ClearPrefix))
                {
                    // Sure to be clear/unencoded
                    Value = rawValue.Substring(ClearPrefix.Length);
                }
                else
                {
                    // Should be Base64
                    try
                    {
                        var clearBytes = Convert.FromBase64String(rawValue);
                        Value = Encoding.UTF8.GetString(clearBytes);
                    }
                    catch
                    {
                        _log.Error("Unable to decode configuration value, use the prefix {prefix} to input clear text", ClearPrefix);
                        Error = true;
                    }
                }
            }
        }

        /// <summary>
        /// Encrypted value should be used when the "EncryptConfig" setting is true
        /// </summary>
        internal string ProtectedValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Error)
                {
                    return Value;
                }
                else
                {
                    return EncryptedPrefix + Protect(Value);
                }
            }
        }

        /// <summary>
        /// Encoded value should be used when the "EncryptConfig" setting is false
        /// </summary>
        internal string EncodedValue
        {
            get
            {
                if (string.IsNullOrEmpty(Value) || Error)
                {
                    return Value;
                }
                else
                {
                    return Encode(Value);
                }
            }
        }

        /// <summary>
        /// Base64 encode a string
        /// </summary>
        /// <param name="clearText"></param>
        /// <returns></returns>
        private string Encode(string clearText)
        {
            var clearBytes = Encoding.UTF8.GetBytes(clearText);
            return Convert.ToBase64String(clearBytes);
        }

        /// <summary>
        /// Encrypt and Base64-encode a string
        /// </summary>
        /// <param name="clearText"></param>
        /// <param name="optionalEntropy"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private string Protect(string clearText, string optionalEntropy = null, DataProtectionScope scope = DataProtectionScope.LocalMachine)
        {
            var clearBytes = Encoding.UTF8.GetBytes(clearText);
            var entropyBytes = string.IsNullOrEmpty(optionalEntropy)
                ? null
                : Encoding.UTF8.GetBytes(optionalEntropy);
            var encryptedBytes = ProtectedData.Protect(clearBytes, entropyBytes, scope);
            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Base64-decode and decrypt a string
        /// </summary>
        /// <param name="clearText"></param>
        /// <param name="optionalEntropy"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        private string Unprotect(string encryptedText, string optionalEntropy = null, DataProtectionScope scope = DataProtectionScope.LocalMachine)
        {
            if (encryptedText == null)
            {
                return null;
            }
            byte[] clearBytes = null;
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var entropyBytes = string.IsNullOrEmpty(optionalEntropy)
                ? null
                : Encoding.UTF8.GetBytes(optionalEntropy);
            clearBytes = ProtectedData.Unprotect(encryptedBytes, entropyBytes, scope);
            return Encoding.UTF8.GetString(clearBytes);
        }
    }
}
