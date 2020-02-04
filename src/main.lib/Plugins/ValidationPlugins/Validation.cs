using ACMESharp.Authorizations;
using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    /// <summary>
    /// Base implementation for all validation plugins
    /// </summary>
    public abstract class Validation<TChallenge> : IValidationPlugin where TChallenge : class, IChallengeValidationDetails
    {
        public bool HasChallenge => _challenge != null;
        public TChallenge Challenge 
        {
            get
            {
                if (_challenge == null)
                {
                    throw new InvalidOperationException();
                }
                return _challenge;
            }
        }
        private TChallenge? _challenge;


        /// <summary>
        /// Handle the challenge
        /// </summary>
        /// <param name="challenge"></param>
        public async Task PrepareChallenge(IChallengeValidationDetails challenge)
        {
            if (challenge is TChallenge typed)
            {
                _challenge = typed;
                await PrepareChallenge();
            } 
            else
            {
                throw new InvalidOperationException("Unexpected challenge type");
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

        /// <summary>
        /// Is the plugin currently disabled
        /// </summary>
        public virtual bool Disabled => false;
    }
}
