using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class StorePluginOptions : PluginOptions
    {
        public override string Name => throw new NotImplementedException();
        public override string Description => throw new NotImplementedException();
        public override Type Instance => throw new NotImplementedException();
        public bool? KeepExisting { get; set; }
    }

    public abstract class StorePluginOptions<T> : StorePluginOptions where T : IStorePlugin
    {
        public abstract override string Name { get; }
        public abstract override string Description { get; }

        public override void Show(IInputService input)
        {
            input.Show("Store");
            input.Show("Plugin", $"{Name} - ({Description})", level: 1);
            if (KeepExisting == true)
            {
                input.Show("KeepExisting", "Yes", level: 1);
            }
        }

        public override Type Instance => typeof(T);
    }
}
