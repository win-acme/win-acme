using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface ISecretService
    {
        /// <summary>
        /// Make references to this provider unique from 
        /// references in other providers
        /// </summary>
        string Prefix { get; }

        /// <summary>
        /// (Re)save to disk to support encrypt/decrypt operations
        /// </summary>
        void Encrypt();

        /// <summary>
        /// List available keys in the system
        /// </summary>
        IEnumerable<string> ListKeys();

        /// <summary>
        /// Get a secret from the vault
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string? GetSecret(string? key);

        /// <summary>
        /// Put a secret in the vault
        /// </summary>
        /// <param name="key"></param>
        /// <param name="secret"></param>
        void PutSecret(string key, string secret);

        /// <summary>
        /// Delete secret from the store
        /// </summary>
        /// <param name="key"></param>
        void DeleteSecret(string key);
    }
}
