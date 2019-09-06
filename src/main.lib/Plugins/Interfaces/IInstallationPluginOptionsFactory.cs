using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;

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
        InstallationPluginOptions Aquire(Target target, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information need for installation (unattended)
        /// </summary>
        /// <param name="target"></param>
        InstallationPluginOptions Default(Target target);

        /// <summary>
        /// Can this plugin be used?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanInstall(IEnumerable<Type> storeTypes);
    }
}
