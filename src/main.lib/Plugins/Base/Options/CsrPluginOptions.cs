using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class CsrPluginOptions : PluginOptions
    {
        public bool? OcspMustStaple { get; set; }
        public bool? ReusePrivateKey { get; set; }

        public override void Show(IInputService input)
        {
            if (OcspMustStaple == true)
            {
                input.Show("OcspMustStaple", "Yes");
            }
            if (ReusePrivateKey == true)
            {
                input.Show("ReusePrivateKey", "Yes");
            }
        }
    }
}
