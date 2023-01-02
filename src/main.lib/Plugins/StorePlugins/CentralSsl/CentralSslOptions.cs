using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslOptions : StorePluginOptions<CentralSsl>
    {
        /// <summary>
        /// Path to the Central Ssl store
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// PfxFile password
        /// </summary>
        [JsonPropertyName("PfxPasswordProtected")]
        public ProtectedString? PfxPassword { get; set; }

        internal const string PluginName = "CentralSsl";
        
        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Path", string.IsNullOrEmpty(Path) ? "[Default from settings.json]" : Path, level: 2);
            input.Show("Password", string.IsNullOrEmpty(PfxPassword?.Value) ? "[Default from settings.json]" : PfxPassword.DisplayValue, level: 2);
        }
    }
}
