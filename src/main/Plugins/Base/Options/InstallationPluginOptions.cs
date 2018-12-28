using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class InstallationPluginOptions : PluginOptions { }

    public class InstallationPluginOptions<T> : InstallationPluginOptions where T : IInstallationPlugin
    {
        public override void Show(IInputService input)
        {
            input.Show("Installation", $"{Name} - ({Description})");
        }

        public override Type Instance => typeof(T);
    }
}
