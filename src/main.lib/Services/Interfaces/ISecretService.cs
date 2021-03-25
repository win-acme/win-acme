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
        void Save();

        /// <summary>
        /// Get a secret from the vault
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        string? GetSecret(string? identifier);

        /// <summary>
        /// Put a secret in the vault
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="secret"></param>
        void PutSecret(string identifier, string secret);
    }
}
