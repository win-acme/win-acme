using ACMESharp;
using LetsEncrypt.ACME.Simple.Services;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.Interfaces
{
    /// <summary>
    /// Instance interface
    /// </summary>
    public interface IValidationPlugin
    {
        /// <summary>
        /// Prepare challenge
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="challenge"></param>
        /// <returns></returns>
        Action<AuthorizationState> PrepareChallenge(AuthorizeChallenge challenge, string identifier);
    }
}
