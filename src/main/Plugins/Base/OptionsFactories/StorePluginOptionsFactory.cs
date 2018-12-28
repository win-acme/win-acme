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
        public StorePluginOptionsFactory(ILogService log) : base(log) { }
        public abstract TOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(IOptionsService optionsService);

        Type IStorePluginOptionsFactory.OptionsType { get => typeof(TOptions); }
        StorePluginOptions IStorePluginOptionsFactory.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(optionsService, inputService, runLevel);
        }
        StorePluginOptions IStorePluginOptionsFactory.Default(IOptionsService optionsService)
        {
            return Default(optionsService);
        }
    }



}
