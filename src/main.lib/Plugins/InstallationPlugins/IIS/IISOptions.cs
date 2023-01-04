using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class IISOptions : InstallationPluginOptions
    {
        public long? SiteId { get; set; }
        public string? NewBindingIp { get; set; }
        public int? NewBindingPort { get; set; }

        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            if (SiteId != null)
            {
                input.Show("SiteId", SiteId.ToString(), level: 2);
            }
            if (NewBindingIp != null)
            {
                input.Show("NewBindingIp", NewBindingIp, level: 2);
            }
            if (NewBindingPort != null)
            {
                input.Show("NewBindingPort", NewBindingPort.ToString(), level: 2);
            }
        }
    }
}
