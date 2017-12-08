using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.Interfaces
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
        void Aquire(Target target, IOptionsService optionsService, IInputService inputService);

        /// <summary>
        /// Check information need for validation (unattended)
        /// </summary>
        /// <param name="target"></param>
        void Default(Target target, IOptionsService optionsService);
    }
}
