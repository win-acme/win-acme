using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    internal class PemFilesOptions : StorePluginOptions
    {
        /// <summary>
        /// PemFiles password
        /// </summary>
        public ProtectedString? PemPassword { get; set; }

        /// <summary>
        /// Path to the .pem directory
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Name to use
        /// </summary>
        public string? FileName { get; set; }

        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Path", Path, level: 1);
            input.Show("Password", string.IsNullOrEmpty(PemPassword?.Value) ? "[Default from settings.json]" : PemPassword.DisplayValue, level: 2);
        }
    }
}
