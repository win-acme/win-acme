using ACMESharp.Authorizations;
using PKISharp.WACS.Context;
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
        /// Commit changes
        /// </summary>
        /// <returns></returns>
        public abstract Task Commit();

        public abstract Task CleanUp();

        /// <summary>
        /// No parallelism by default
        /// </summary>
        public virtual ParallelOperations Parallelism => ParallelOperations.None;
    }
}
