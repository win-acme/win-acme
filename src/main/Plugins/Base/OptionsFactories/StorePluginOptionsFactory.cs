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
        public abstract TOptions Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(IArgumentsService arguments);

        StorePluginOptions IStorePluginOptionsFactory.Aquire(IArgumentsService arguments, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(arguments, inputService, runLevel);
        }
        StorePluginOptions IStorePluginOptionsFactory.Default(IArgumentsService arguments)
        {
            return Default(arguments);
        }
    }



}
