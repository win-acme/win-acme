using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// TargetPluginFactory interface
    /// </summary>
    public interface ITargetPluginOptionsFactory : IPluginOptionsFactory
    {
        /// <summary>
        /// Hide when it cannot be chosen
        /// </summary>
        bool Hidden { get; }

        /// <summary>
        /// Type used for storing this plugins configuration options
        /// </summary>
        Type OptionsType { get; }

        /// <summary>
        /// Check or get information needed for target (interactive)
        /// </summary>
        /// <param name="target"></param>
        TargetPluginOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information needed for target (unattended)
        /// </summary>
        /// <param name="target"></param>
        TargetPluginOptions Default(IOptionsService optionsService);
    }
}
