using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class StorePluginOptions : PluginOptions
    {
        public bool KeepExisting { get; set; }
    }

    public class StorePluginOptions<T> : StorePluginOptions where T : IStorePlugin
    {
        public override void Show(IInputService input)
        {
            input.Show("Store", $"{Name} - ({Description})");
            if (KeepExisting)
            {
                input.Show("- KeepExisting", "Yes");
            }
        }

        public override Type Instance => typeof(T);


    }
}
