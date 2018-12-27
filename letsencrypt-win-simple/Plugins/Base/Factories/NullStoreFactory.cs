using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullStoreFactory : IStorePluginFactory, INull
    {
        string IHasName.Name => string.Empty;
        string IHasName.Description => string.Empty;
        Type IHasType.Instance => typeof(object);
        bool IHasName.Match(string name) => false;
        StorePluginOptions IStorePluginFactory.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel) => null;
        StorePluginOptions IStorePluginFactory.Default(IOptionsService optionsService) => null;
        Type IStorePluginFactory.OptionsType => null;
    }
}
