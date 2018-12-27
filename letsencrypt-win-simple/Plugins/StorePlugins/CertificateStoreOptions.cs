using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class CertificateStorePluginOptions : StorePluginOptions<CertificateStore>
    {
        internal const string PluginName = "Store";
        public override string Name { get => PluginName; }
        public override string Description { get => "Windows Certificate Store"; }

        /// <summary>
        /// Name of the certificate store to use
        /// </summary>
        public string StoreName { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);
            if (!string.IsNullOrEmpty(StoreName))
            {
                input.Show("- Store", StoreName);
            }
        }
    }
}
