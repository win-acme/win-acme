using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// DnsMadeEasy DNS validation
    /// </summary>
    internal class DnsMadeEasyOptionsFactory : PluginOptionsFactory<DnsMadeEasyOptions>
    {
        private readonly ArgumentsInputService _arguments;

        public DnsMadeEasyOptionsFactory(ArgumentsInputService arguments) => 
            _arguments = arguments;

        private ArgumentResult<ProtectedString?> ApiKey => _arguments.
            GetProtectedString<DnsMadeEasyArguments>(a => a.ApiKey).
            Required();

        private ArgumentResult<ProtectedString?> ApiSecret => _arguments.
            GetProtectedString<DnsMadeEasyArguments>(a => a.ApiSecret);

        public override async Task<DnsMadeEasyOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            return new DnsMadeEasyOptions()
            {
                ApiKey = await ApiKey.Interactive(input).GetValue(),
                ApiSecret = await ApiSecret.Interactive(input).GetValue(),
            };
        }

        public override async Task<DnsMadeEasyOptions?> Default()
        {
            return new DnsMadeEasyOptions()
            {
                ApiKey = await ApiKey.GetValue(),
                ApiSecret = await ApiSecret.GetValue(),
            };
        }
    }
}
