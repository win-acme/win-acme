using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class StorePluginOptions : PluginOptions
    {
        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override Type Instance => throw new NotImplementedException();
        public bool? KeepExisting { get; set; }
    }

    public abstract class StorePluginOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : 
        StorePluginOptions where T : IStorePlugin
    {
        public override void Show(IInputService input)
        {
            if (KeepExisting == true)
            {
                input.Show("KeepExisting", "Yes", level: 1);
            }
        }
        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override Type Instance => typeof(T);
    }
}
