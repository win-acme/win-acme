using ACMESharp.Authorizations;
using PKISharp.WACS.Plugins.Interfaces;
using System;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for all validation plugins
    /// </summary>
    public abstract class Validation<TChallenge> : IValidationPlugin where TChallenge : IChallengeValidationDetails
    {
        protected TChallenge _challenge;

        /// <summary>
        /// Handle the challenge
        /// </summary>
        /// <param name="challenge"></param>
        public void PrepareChallenge(IChallengeValidationDetails challenge)
        {
            if (challenge.GetType() != typeof(TChallenge))
            {
                throw new InvalidOperationException();
            }
            else
            {
                _challenge = (TChallenge)challenge;
                PrepareChallenge();
            }
        }

        /// <summary>
        /// Handle the challenge
        /// </summary>
        /// <param name="challenge"></param>
        public abstract void PrepareChallenge();

        /// <summary>
        /// Clean up after validation
        /// </summary>
        public abstract void CleanUp();

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    CleanUp();
                }
                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion

    }
}
