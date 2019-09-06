using System;
using ACMESharp.Authorizations;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for all validation plugins
    /// </summary>
    public abstract class Validation<TOptions, TChallenge> : IValidationPlugin where TChallenge : IChallengeValidationDetails
    {
        protected ILogService _log;
        protected string _identifier;
        protected TChallenge _challenge;
        protected TOptions _options;

        public Validation(ILogService logService, TOptions options, string identifier)
        {
            _log = logService;
            _identifier = identifier;
            _options = options;
        }

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

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }
}
