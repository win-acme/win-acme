using ACMESharp.Authorizations;
using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Threading.Tasks;

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
        public async Task PrepareChallenge(IChallengeValidationDetails challenge)
        {
            if (challenge.GetType() != typeof(TChallenge))
            {
                throw new InvalidOperationException();
            }
            else
            {
                _challenge = (TChallenge)challenge;
                await PrepareChallenge();
            }
        }

        /// <summary>
        /// Handle the challenge
        /// </summary>
        /// <param name="challenge"></param>
        public abstract Task PrepareChallenge();

        /// <summary>
        /// Clean up after validation
        /// </summary>
        public abstract Task CleanUp();

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
