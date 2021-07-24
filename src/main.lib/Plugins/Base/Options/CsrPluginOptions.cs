using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class CsrPluginOptions : PluginOptions
    {
        public override string Name => throw new NotImplementedException();
        public override string Description => throw new NotImplementedException();
        public override Type Instance => throw new NotImplementedException();
        public bool? OcspMustStaple { get; set; }
        public bool? ReusePrivateKey { get; set; }
    }

    public abstract class CsrPluginOptions<TPlugin> : CsrPluginOptions where TPlugin : ICsrPlugin
    {
        public abstract override string Name { get; }
        public abstract override string Description { get; }
        public override void Show(IInputService input)
        {
            input.Show(null, "[CSR]");
            input.Show("Plugin", $"{Name} - ({Description})", level: 1);
            if (OcspMustStaple == true)
            {
                input.Show("OcspMustStaple", "Yes");
            }
            if (ReusePrivateKey == true)
            {
                input.Show("ReusePrivateKey", "Yes");
            }
        }
        public override Type Instance => typeof(TPlugin);
    }
}
