using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IValidationPluginOptionsFactory : IPluginOptionsFactory
    {
        /// <summary>
        /// Type of challange
        /// </summary>
        string ChallengeType { get; }

        /// <summary>
        /// Check or get information needed for store (interactive)
        /// </summary>
        /// <param name="target"></param>
        ValidationPluginOptions Aquire(Target target, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information needed for store (unattended)
        /// </summary>
        /// <param name="target"></param>
        ValidationPluginOptions Default(Target target);

        /// <summary>
        /// Is the validation option available for a specific target?
        /// Used to rule out HTTP validation for wildcard certificates
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanValidate(Target target);
    }
}
