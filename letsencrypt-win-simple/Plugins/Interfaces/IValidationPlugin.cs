using ACMESharp;
using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Instance interface
    /// </summary>
    public interface IValidationPlugin : IDisposable
    {
        /// <summary>
        /// Prepare challenge
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="challenge"></param>
        /// <returns></returns>
        void PrepareChallenge(AuthorizeChallenge challenge);
    }
}
