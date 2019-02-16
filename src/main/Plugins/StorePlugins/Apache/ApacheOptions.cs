using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    [Plugin("e57c70e4-cd60-4ba6-80f6-a41703e21031")]
    internal class ApacheOptions : StorePluginOptions<Apache>
    {
        /// <summary>
        /// Path to the Apache certificate directory
        /// </summary>
        public string Path { get; set; }
        public override string Name { get => "Apache"; }
        public override string Description { get => "Write .pem files to folder (Apache)"; }

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
