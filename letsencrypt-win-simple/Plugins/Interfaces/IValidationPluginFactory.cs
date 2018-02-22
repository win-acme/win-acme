using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IValidationPluginFactory : IHasName, IHasType
    {
        /// <summary>
        /// Type of challange
        /// </summary>
        string ChallengeType { get; }

        /// <summary>
        /// Is this plugin capable of validating the target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanValidate(Target target);

        /// <summary>
        /// Check or get information need for validation (interactive)
        /// </summary>
        /// <param name="target"></param>
        void Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information need for validation (unattended)
        /// </summary>
        /// <param name="target"></param>
        void Default(Target target, IOptionsService optionsService);

        /// <summary>
        /// Hide when it cannot be chosen
        /// </summary>
        bool Hidden { get; }
    }
}
