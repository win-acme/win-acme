using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class CsrPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        ICsrPluginOptionsFactory
        where TPlugin : ICsrPlugin
        where TOptions : CsrPluginOptions, new()
    {
        public abstract TOptions Aquire(IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default();
        CsrPluginOptions ICsrPluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel)
        {
            return Aquire(inputService, runLevel);
        }
        CsrPluginOptions ICsrPluginOptionsFactory.Default()
        {
            return Default();
        }
    }
}