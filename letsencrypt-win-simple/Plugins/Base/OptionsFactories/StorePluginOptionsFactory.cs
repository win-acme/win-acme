using System;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// StorePluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class BaseStorePluginFactory<TPlugin, TOptions> :
        BasePluginFactory<TPlugin>,
        IStorePluginOptionsFactory, IHasName
        where TPlugin : IStorePlugin
        where TOptions : StorePluginOptions, new()
    {
        public BaseStorePluginFactory(ILogService log) : base(log, "", "") { }
        public abstract TOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(IOptionsService optionsService);

        // TODO: Remove
        string IHasName.Name => (new TOptions()).Name;
        string IHasName.Description => (new TOptions()).Description;
        public override bool Match(string name)
        {
            return string.Equals(name, (new TOptions()).Name, StringComparison.InvariantCultureIgnoreCase);
        }

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
