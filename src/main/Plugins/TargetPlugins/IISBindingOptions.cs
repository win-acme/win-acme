using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    class IISBindingOptions : TargetPluginOptions<IISBinding>
    {
        public override string Name => "IISBinding";
        public override string Description => "Single binding of an IIS site";

        /// <summary>
        /// Restrict search to a specific site
        /// </summary>
        public long SiteId { get; set; }

        /// <summary>
        /// Host name of the binding to look for
        /// </summary>
        public string Host { get; set; }

        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Host", Host, level: 1);
            input.Show("SiteId", SiteId.ToString(), level: 1);
        }
    }
}
