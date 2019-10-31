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
    public abstract class CsrPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        ICsrPluginOptionsFactory
        where TPlugin : ICsrPlugin
        where TOptions : CsrPluginOptions, new()
    {
        public abstract Task<TOptions> Aquire(IInputService inputService, RunLevel runLevel);
        public abstract Task<TOptions> Default();
        async Task<CsrPluginOptions> ICsrPluginOptionsFactory.Aquire(IInputService inputService, RunLevel runLevel) => await Aquire(inputService, runLevel);
        async Task<CsrPluginOptions> ICsrPluginOptionsFactory.Default() => await Default();
    }
}