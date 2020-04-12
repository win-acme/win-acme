using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// OrderPluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class OrderPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        IOrderPluginOptionsFactory
        where TPlugin : IOrderPlugin
        where TOptions : OrderPluginOptions, new()
    {
        public abstract Task<TOptions> Aquire(IInputService inputService, RunLevel runLevel);
        public abstract Task<TOptions> Default();
        async Task<OrderPluginOptions?> IPluginOptionsFactory<OrderPluginOptions>.Aquire(IInputService inputService, RunLevel runLevel) => await Aquire(inputService, runLevel);
        async Task<OrderPluginOptions?> IPluginOptionsFactory<OrderPluginOptions>.Default() => await Default();
    }
}