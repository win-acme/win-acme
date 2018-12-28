using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullStoreFactory : IStorePluginOptionsFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        bool IHasName.Match(string name) => false;
        StorePluginOptions IStorePluginOptionsFactory.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel) => null;
        StorePluginOptions IStorePluginOptionsFactory.Default(IOptionsService optionsService) => null;
        Type IStorePluginOptionsFactory.OptionsType => null;
    }
}
