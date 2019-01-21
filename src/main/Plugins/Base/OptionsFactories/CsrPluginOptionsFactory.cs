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
        public CsrPluginOptionsFactory(ILogService log) : base(log) { }
        Type ICsrPluginOptionsFactory.OptionsType { get => typeof(TOptions); }
        public abstract TOptions Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel);
        public abstract TOptions Default(IOptionsService optionsService);
        CsrPluginOptions ICsrPluginOptionsFactory.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            return Aquire(optionsService, inputService, runLevel);
        }
        CsrPluginOptions ICsrPluginOptionsFactory.Default(IOptionsService optionsService)
        {
            return Default(optionsService);
        }
    }
}