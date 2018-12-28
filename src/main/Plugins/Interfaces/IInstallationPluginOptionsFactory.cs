using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Handles configuration
    /// </summary>
    public interface IInstallationPluginOptionsFactory : IPluginFactory
    {
        /// <summary>
        /// Check or get information need for installation (interactive)
        /// </summary>
        /// <param name="target"></param>
        InstallationPluginOptions Aquire(ScheduledRenewal renewal, IOptionsService optionsService, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information need for installation (unattended)
        /// </summary>
        /// <param name="target"></param>
        InstallationPluginOptions Default(ScheduledRenewal renewal, IOptionsService optionsService);

        /// <summary>
        /// Can this plugin be used?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanInstall();
    }
}
