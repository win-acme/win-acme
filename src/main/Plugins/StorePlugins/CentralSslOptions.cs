using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CentralSslOptions : StorePluginOptions<CentralSsl>
    {
        /// <summary>
        /// Path to the Central Ssl store
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Encrypted (if enabled) version of the PfxFile password
        /// </summary>
        public string PfxPasswordProtected { get; set; }

        /// <summary>
        /// Plain text readable version of the PfxFile password
        /// </summary>
        [JsonIgnore]
        public string PfxPassword
        {
            get => PfxPasswordProtected.Unprotect();
            set => PfxPasswordProtected = value.Protect();
        }

        public override string Name { get => "CCS"; }
        public override string Description { get => "IIS Central Certificate Store"; }

        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Path", Path, level:1);
        }
    }
}
