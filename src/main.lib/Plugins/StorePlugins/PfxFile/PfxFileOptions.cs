using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [Plugin("2a2c576f-7637-4ade-b8db-e8613b0bb33e")]
    internal class PfxFileOptions : StorePluginOptions<PfxFile>
    {
        /// <summary>
        /// Path to the folder
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// PfxFile password
        /// </summary>
        public ProtectedString? PfxPassword { get; set; }

        internal const string PluginName = "PfxFile";
        public override string Name => PluginName;
        public override string Description => "PFX archive";

        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Path", string.IsNullOrEmpty(Path) ? "[Default from settings.json]" : Path, level: 2);
            input.Show("Password", string.IsNullOrEmpty(PfxPassword?.Value) ? "[Default from settings.json]" : new string('*', PfxPassword.Value.Length), level: 2);
        }
    }
}
