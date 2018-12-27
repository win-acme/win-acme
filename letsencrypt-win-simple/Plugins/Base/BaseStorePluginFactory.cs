using System;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base
{
    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class BaseStorePluginFactory<TPlugin, TOptions> :
        BasePluginFactory<TPlugin>,
        IStorePluginFactory, IHasName
        where TPlugin : IStorePlugin
        where TOptions : StorePluginOptions, new()
    {
        public BaseStorePluginFactory(ILogService log, string name, string description = null) : base(log, name, description) { }
        public abstract TOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(IOptionsService optionsService);

        string IHasName.Name => (new TOptions()).Name;
        string IHasName.Description => (new TOptions()).Description;

        Type IStorePluginFactory.OptionsType { get => typeof(TOptions); }
        StorePluginOptions IStorePluginFactory.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(optionsService, inputService, runLevel);
        }
        StorePluginOptions IStorePluginFactory.Default(IOptionsService optionsService)
        {
            return Default(optionsService);
        }
    }

}
