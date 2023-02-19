using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class StorePluginOptions : PluginOptions
    {
        public bool? KeepExisting { get; set; }

        public override void Show(IInputService input)
        {
            if (KeepExisting == true)
            {
                input.Show("KeepExisting", "Yes", level: 1);
            }
        }
    }
}
