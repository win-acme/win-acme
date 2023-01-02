using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class ValidationPluginOptions : PluginOptions
    {
        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override Type Instance => throw new NotImplementedException();
    }

    public abstract class ValidationPluginOptions<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T> : 
        ValidationPluginOptions where T : IValidationPlugin
    {
        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public override Type Instance => typeof(T);
    }
}
