using ACMESharp.Authorizations;
using ACMESharp.Protocol.Resources;
using Autofac;
using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;
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
        Task PrepareChallenge(ValidationContext context);

        /// <summary>
        /// Clean up after validation attempt
        /// </summary>
       Task CleanUp(ValidationContext context);
    }

    public class ValidationContext
    {
        public ValidationContext(
            ILifetimeScope scope, 
            Authorization authorization, 
            TargetPart targetPart, 
            string challengeType, 
            string pluginName)
        {
            Identifier = authorization.Identifier.Value;
            TargetPart = targetPart;
            Authorization = authorization;
            Scope = scope;
            ChallengeType = challengeType;
            PluginName = pluginName;
        }
        public ILifetimeScope Scope { get; }
        public string Identifier { get; }
        public string ChallengeType { get; }
        public string PluginName { get; }
        public TargetPart TargetPart { get; }
        public Authorization Authorization { get; }
        public Challenge? Challenge { get; set; }
        public IChallengeValidationDetails? ChallengeDetails { get; set; }
        public IValidationPlugin? ValidationPlugin { get; set; }
        public bool? Success { get; set; }
        public List<string> ErrorMessages { get; } = new List<string>();
        public void AddErrorMessage(string? value, bool fatal = true)
        {
            if (value != null)
            {
                if (!ErrorMessages.Contains(value))
                {
                    ErrorMessages.Add(value);
                }
            }
            if (fatal)
            {
                Success = false;
            }
        }
    }

}
