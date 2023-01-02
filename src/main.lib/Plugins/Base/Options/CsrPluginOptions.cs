using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class CsrPluginOptions : PluginOptions
    {
        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override Type Instance => throw new NotImplementedException();
        public bool? OcspMustStaple { get; set; }
        public bool? ReusePrivateKey { get; set; }
    }

    public abstract class CsrPluginOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TPlugin> : 
        CsrPluginOptions where TPlugin : ICsrPlugin
    {
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
        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override Type Instance => typeof(TPlugin);
    }
}
