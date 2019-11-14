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
        public override string Description => "Multiple IIS bindings";

        /// <summary>
        /// Csv list
        /// </summary>
        public string Hosts { get; set; }

        /// <summary>
        /// Search string to select hosts
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Regular expression to select hosts
        /// </summary>
        public Regex Regex { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);

            if (!string.IsNullOrWhiteSpace(Pattern))
            {
                input.Show(nameof(Pattern), Pattern, level: 1);
            }
            if (!string.IsNullOrWhiteSpace(Hosts))
            {
                input.Show(nameof(Hosts), Hosts, level: 1);
            }
            if (Regex != default)
            {
                input.Show(nameof(Regex), Regex.ToString(), level: 1);
            }
        }
    }
}
