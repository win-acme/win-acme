using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// CsrPluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class CsrPluginOptionsFactory<TPlugin, TOptions> :
        PluginOptionsFactory<TPlugin, TOptions>,
        ICsrPluginOptionsFactory
        where TPlugin : ICsrPlugin
        where TOptions : CsrPluginOptions, new()
    {
        protected ArgumentsInputService _arguments { get; set; }
        protected CsrPluginOptionsFactory(ArgumentsInputService arguments) => _arguments = arguments;
        protected ArgumentResult<bool?> OcspMustStaple => _arguments.
            GetBool<CsrArguments>(x => x.OcspMustStaple).
            WithDefault(false).
            DefaultAsNull();

        protected ArgumentResult<bool?> ReusePrivateKey => _arguments.
            GetBool<CsrArguments>(x => x.ReusePrivateKey).
            WithDefault(false).
            DefaultAsNull();

        public abstract Task<TOptions> Aquire(IInputService inputService, RunLevel runLevel);
        public abstract Task<TOptions> Default();
        async Task<CsrPluginOptions?> IPluginOptionsFactory<CsrPluginOptions>.Aquire(IInputService inputService, RunLevel runLevel) => await Aquire(inputService, runLevel);
        async Task<CsrPluginOptions?> IPluginOptionsFactory<CsrPluginOptions>.Default() => await Default();
    }
}