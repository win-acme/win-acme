using LetsEncrypt.ACME.Simple.Services;

namespace LetsEncrypt.ACME.Simple.Plugins.Interfaces
{
    /// <summary>
    /// Handles configuration
    /// </summary>
    public interface IInstallationPluginFactory : IHasName, IHasType
    {
        /// <summary>
        /// Check or get information need for installation (interactive)
        /// </summary>
        /// <param name="target"></param>
        void Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information need for installation (unattended)
        /// </summary>
        /// <param name="target"></param>
        void Default(ScheduledRenewal renewal, IOptionsService optionsService);

        /// <summary>
        /// Can this plugin be used for this specific target?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanInstall(ScheduledRenewal renewal);
    }
}
