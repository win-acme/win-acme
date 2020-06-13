using ACMESharp.Authorizations;
using PKISharp.WACS.DomainObjects;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Instance interface
    /// </summary>
    public interface IValidationPlugin : IPlugin
    {
        /// <summary>
        /// Prepare challenge
        /// </summary>
        /// <param name="options"></param>
        /// <param name="target"></param>
        /// <param name="challenge"></param>
        /// <returns></returns>
        Task PrepareChallenge(ValidationContext context, IChallengeValidationDetails challenge);

        /// <summary>
        /// Clean up after validation attempt
        /// </summary>
       Task CleanUp(ValidationContext context, IChallengeValidationDetails challenge);
    }

    public class ValidationContext
    {
        public ValidationContext(string identifier, TargetPart targetPart)
        {
            Identifier = identifier;
            TargetPart = targetPart;
        }
        public string Identifier { get; set; }
        public TargetPart TargetPart { get; set; }
    }

}
