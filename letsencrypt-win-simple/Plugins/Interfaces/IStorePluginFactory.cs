using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    /// <summary>
    /// StorePluginFactory interface
    /// </summary>
    public interface IStorePluginFactory : IHasName, IHasType
    {
        Type OptionsType { get; }

        /// <summary>
        /// Check or get information needed for store (interactive)
        /// </summary>
        /// <param name="target"></param>
        StorePluginOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information needed for store (unattended)
        /// </summary>
        /// <param name="target"></param>
        StorePluginOptions Default(IOptionsService optionsService);
    }
}
