using System;
using System.Security.Cryptography;
using System.Text;

namespace PKISharp.WACS.Services.Serialization
{
    public class ProtectedString
    {
        internal const string EncryptedPrefix = "enc-";
        internal const string ClearPrefix = "clear-";

        private readonly ILogService _log;
        public bool Error { get; private set; } = false;

        public ProtectedString(string clearValue)
        {
            Value = clearValue;
        }

        public ProtectedString(string rawValue, ILogService log)
        {
            _log = log;
            if (!string.IsNullOrEmpty(rawValue))
            {
                Value = rawValue;
            }
            else
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
                        Value = rawValue;
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
                    // Maybe Base64, or may be a normal string
                    try
                    {
                        var clearBytes = Convert.FromBase64String(rawValue);
                        Value = Encoding.UTF8.GetString(clearBytes);
                    }
                    catch
                    {
                        Value = rawValue;
                    }
                }
            }
        }

        public string Value { get; private set; }

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

        string Protect(string clearText, string optionalEntropy = null, DataProtectionScope scope = DataProtectionScope.LocalMachine)
        {
            byte[] clearBytes = Encoding.UTF8.GetBytes(clearText);
            byte[] entropyBytes = string.IsNullOrEmpty(optionalEntropy)
                ? null
                : Encoding.UTF8.GetBytes(optionalEntropy);
            byte[] encryptedBytes = ProtectedData.Protect(clearBytes, entropyBytes, scope);
            return Convert.ToBase64String(encryptedBytes);
        }

        string Unprotect(string encryptedText, string optionalEntropy = null, DataProtectionScope scope = DataProtectionScope.LocalMachine)
        {
            if (encryptedText == null)
            {
                return null;
            }
            byte[] clearBytes = null;
            byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
            byte[] entropyBytes = string.IsNullOrEmpty(optionalEntropy)
                ? null
                : Encoding.UTF8.GetBytes(optionalEntropy);
            clearBytes = ProtectedData.Unprotect(encryptedBytes, entropyBytes, scope);
            return Encoding.UTF8.GetString(clearBytes);
        }
    }
}
