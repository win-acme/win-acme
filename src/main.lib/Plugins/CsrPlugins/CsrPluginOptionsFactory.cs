using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Base.Factories
{
    /// <summary>
    /// CsrPluginFactory base implementation
    /// </summary>
    /// <typeparam name="TPlugin"></typeparam>
    public abstract class CsrPluginOptionsFactory<TOptions> :
        PluginOptionsFactory<TOptions>
        where TOptions : CsrPluginOptions, new()
    {
        protected ArgumentsInputService Arguments { get; set; }
        protected CsrPluginOptionsFactory(ArgumentsInputService arguments) => Arguments = arguments;
        protected ArgumentResult<bool?> OcspMustStaple => Arguments.
            GetBool<CsrArguments>(x => x.OcspMustStaple).
            WithDefault(false).
            DefaultAsNull();

        protected ArgumentResult<bool?> ReusePrivateKey => Arguments.
            GetBool<CsrArguments>(x => x.ReusePrivateKey).
            WithDefault(false).
            DefaultAsNull();

        public override async Task<TOptions?> Default()
        {
            return new TOptions()
            {
                OcspMustStaple = await OcspMustStaple.GetValue(),
                ReusePrivateKey = await ReusePrivateKey.GetValue()
            };
        }
    }
}