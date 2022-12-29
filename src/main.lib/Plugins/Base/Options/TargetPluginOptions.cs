using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class TargetPluginOptions : PluginOptions
    {
        public override string Name => throw new NotImplementedException();
        public override string Description => throw new NotImplementedException();
        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override Type Instance => throw new NotImplementedException();
    }

    public abstract class TargetPluginOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : TargetPluginOptions where T : ITargetPlugin
    {
        public abstract override string Name { get; }
        public abstract override string Description { get; }

        public override void Show(IInputService input)
        {
            input.Show(null, "[Source]");
            input.Show("Plugin", $"{Name} - ({Description})", level: 1);
        }
        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override Type Instance => typeof(T);
    }
}
