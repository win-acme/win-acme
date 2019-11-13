using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    [Plugin("54deb3ee-b5df-4381-8485-fe386054055b")]
    internal class IISBindingsOptions : TargetPluginOptions<IISBindings>
    {
        public override string Name => "IISBindings";
        public override string Description => "Multiple bindings of multiple IIS websites";

        /// <summary>
        /// Search string to select hosts
        /// </summary>
        public string Simple { get; set; }

        /// <summary>
        /// Regular expression to select hosts
        /// </summary>
        public Regex RegEx { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);

            if (!string.IsNullOrWhiteSpace(Simple))
            {
                input.Show(nameof(Simple), Simple, level: 1);
            }

            if (RegEx != default)
            {
                input.Show(nameof(RegEx), RegEx.ToString(), level: 1);
            }
        }
    }
}
