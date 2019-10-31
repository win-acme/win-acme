using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// Handles configuration
    /// </summary>
    public interface IInstallationPluginOptionsFactory : IPluginOptionsFactory
    {
        /// <summary>
        /// Check or get information need for installation (interactive)
        /// </summary>
        /// <param name="target"></param>
        Task<InstallationPluginOptions> Aquire(Target target, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information need for installation (unattended)
        /// </summary>
        /// <param name="target"></param>
        Task<InstallationPluginOptions> Default(Target target);

        /// <summary>
        /// Can this plugin be used?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanInstall(IEnumerable<Type> storeTypes);
    }
}
