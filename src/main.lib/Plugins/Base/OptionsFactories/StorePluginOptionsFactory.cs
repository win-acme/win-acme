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
    public abstract class StorePluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        IStorePluginOptionsFactory
        where TPlugin : IStorePlugin
        where TOptions : StorePluginOptions, new()
    {
        public abstract TOptions Aquire(IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default();

        StorePluginOptions IStorePluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel)
        {
            return Aquire(inputService, runLevel);
        }
        StorePluginOptions IStorePluginOptionsFactory.Default()
        {
            return Default();
        }
    }



}
