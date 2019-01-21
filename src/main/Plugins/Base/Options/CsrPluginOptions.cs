using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class CsrPluginOptions : PluginOptions {}

    public abstract class CsrPluginOptions<T> : CsrPluginOptions where T : ICsrPlugin
    {
        public override abstract string Name { get; }
        public override abstract string Description { get; }

        public override void Show(IInputService input)
        {
            input.Show("CSR");
            input.Show("Plugin", $"{Name} - ({Description})", level: 1);
        }
        public override Type Instance => typeof(T);
    }
}
