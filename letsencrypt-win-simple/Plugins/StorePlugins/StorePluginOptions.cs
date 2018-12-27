using System;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.StorePlugins
{
    public class StorePluginOptions : PluginOptions {}

    public class StorePluginOptions<T> : StorePluginOptions where T : IStorePlugin
    {
        public override void Show(IInputService input)
        {
            input.Show("Store", $"{Name} - ({Description})");
        }

        public override Type Instance => typeof(T);
    }
}
