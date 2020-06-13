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
        /// <summary>
        /// Handle the challenge
        /// </summary>
        /// <param name="challenge"></param>
        public async Task PrepareChallenge(ValidationContext context)
        {
            if (context.ChallengeDetails is TChallenge typed)
            {
                await PrepareChallenge(context, typed);
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
        public abstract Task PrepareChallenge(ValidationContext context, TChallenge typed);

        /// <summary>
        /// Clean up after validation
        /// </summary>
        public async Task CleanUp(ValidationContext context)
        {
            if (context.ChallengeDetails is TChallenge typed)
            {
                await CleanUp(context, typed);
            }
            else
            {
                throw new InvalidOperationException("Unexpected challenge type");
            }
        }

        public abstract Task CleanUp(ValidationContext context, TChallenge typed);

        /// <summary>
        /// Is the plugin currently disabled
        /// </summary>
        public virtual (bool, string?) Disabled => (false, null);
    }
}
