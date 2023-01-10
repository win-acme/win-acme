using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IPluginCapability
    {
        /// <summary>
        /// Indicates whether the plugin is usable in the current context.
        /// </summary>
        /// <returns></returns>
        State State { get; }
    }

    /// <summary>
    /// Handles installation
    /// </summary>
    public interface IInstallationPluginCapability : IPluginCapability
    {
        /// <summary>
        /// Can this plugin be selected given the current other selections.
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        State CanInstall(IEnumerable<Type> storeTypes, IEnumerable<Type> installationTypes);
    }

    /// <summary>
    /// Handles validation
    /// </summary>
    public interface IValidationPluginCapability : IPluginCapability
    {
        /// <summary>
        /// Which type of challenge can this plugin handle
        /// </summary>
        string ChallengeType { get; }
    }
}
