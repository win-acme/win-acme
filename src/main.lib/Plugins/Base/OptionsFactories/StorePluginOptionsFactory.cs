using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

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
        public abstract Task<TOptions> Aquire(IInputService inputService, RunLevel runLevel);
        public abstract Task<TOptions> Default();

        async Task<StorePluginOptions> IStorePluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel) => await Aquire(inputService, runLevel);
        async Task<StorePluginOptions> IStorePluginOptionsFactory.Default() => await Default();
    }



}
