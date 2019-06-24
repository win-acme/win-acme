using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

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
        /// Plain text readable version of the PfxFile password
        /// </summary>
        [JsonProperty(propertyName: "PfxPasswordProtected")]
        [JsonConverter(typeof(ProtectedStringConverter))]
        public string PfxPassword { get; set; }

        internal const string PluginName = "CentralSsl";
        public override string Name { get => PluginName; }
        public override string Description { get => "IIS Central Certificate Store"; }

        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Path", string.IsNullOrEmpty(Path) ? "[Default from settings.config]" : Path, level:2);
            input.Show("Password", string.IsNullOrEmpty(PfxPassword) ? "[Default from settings.config]" : new string('*', PfxPassword.Length), level: 2);
        }
    }
}
