using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [Plugin("af1f77b6-4e7b-4f96-bba5-c2eeb4d0dd42")]
    internal class CentralSslOptions : StorePluginOptions<CentralSsl>
    {
        /// <summary>
        /// Path to the Central Ssl store
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// PfxFile password
        /// </summary>
        [JsonProperty(propertyName: "PfxPasswordProtected")]
        public ProtectedString PfxPassword { get; set; }

        internal const string PluginName = "CentralSsl";
        public override string Name => PluginName;
        public override string Description => "IIS Central Certificate Store (.pfx per domain)";

        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Path", string.IsNullOrEmpty(Path) ? "[Default from settings.config]" : Path, level: 2);
            input.Show("Password", string.IsNullOrEmpty(PfxPassword?.Value) ? "[Default from settings.config]" : new string('*', PfxPassword.Value.Length), level: 2);
        }
    }
}
