using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface ICsrPluginOptionsFactory : IPluginOptionsFactory
    {
        /// <summary>
        /// Type used for storing this plugins configuration options
        /// </summary>
        Type OptionsType { get; }

        /// <summary>
        /// Check or get information needed for store (interactive)
        /// </summary>
        /// <param name="target"></param>
        CsrPluginOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel);

        /// <summary>
        /// Check information needed for store (unattended)
        /// </summary>
        /// <param name="target"></param>
        CsrPluginOptions Default(IOptionsService optionsService);
    }
}
