using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories.Null
{
    /// <summary>
    /// Null implementation
    /// </summary>
    internal class NullStoreOptionsFactory : IStorePluginOptionsFactory, INull
    {
        Type IPluginOptionsFactory.InstanceType => typeof(object);
        Type IPluginOptionsFactory.OptionsType => typeof(object);
        Task<StorePluginOptions> IStorePluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel) => null;
        Task<StorePluginOptions> IStorePluginOptionsFactory.Default() => null;
        string IPluginOptionsFactory.Name => "None";
        bool IPluginOptionsFactory.Disabled => true;
        string IPluginOptionsFactory.Description => "No additional storage steps required";
        bool IPluginOptionsFactory.Match(string name) => false;
        int IPluginOptionsFactory.Order => int.MaxValue;
    }
}
