using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullStoreOptionsFactory : IStorePluginOptionsFactory, INull
    {
        Type IHasType.InstanceType => typeof(object);
        Type IHasType.OptionsType => typeof(object);
        StorePluginOptions IStorePluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel) => null;
        StorePluginOptions IStorePluginOptionsFactory.Default() => null;
        string IHasName.Name => "None";
        string IHasName.Description => "No additional storage steps required";
        bool IHasName.Match(string name) => false;
        public int Order => int.MaxValue;
    }
}
