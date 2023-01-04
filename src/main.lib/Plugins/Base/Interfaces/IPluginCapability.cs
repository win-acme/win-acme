using System.Collections.Generic;
using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IPluginCapability
    {
        /// <summary>
        /// Indicates whether the plugin is currently disabled and why
        /// </summary>
        /// <returns></returns>
        (bool, string?) Disabled { get; }
    }

    /// <summary>
    /// Handles configuration
    /// </summary>
    public interface IInstallationPluginCapability : IPluginCapability
    {
        /// <summary>
        /// Can this plugin be used?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        (bool, string?) CanInstall(IEnumerable<Type> storeTypes, IEnumerable<Type> installationTypes);
    }

    /// <summary>
    /// Handles configuration
    /// </summary>
    public interface IOrderPluginCapability : IPluginCapability
    {
        /// <summary>
        /// Can this plugin be used?
        /// </summary>
        bool CanProcess();
    }

    /// <summary>
    /// Handles configuration
    /// </summary>
    public interface IValidationPluginCapability : IPluginCapability
    {
        /// <summary>
        /// Can this plugin be used?
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        bool CanValidate();
    }
}
