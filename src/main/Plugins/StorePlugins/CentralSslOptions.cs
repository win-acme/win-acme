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
