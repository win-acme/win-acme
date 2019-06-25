using ACMESharp.Authorizations;
using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class ValidationPluginOptions : PluginOptions
    {
        [JsonIgnore]
        public virtual string ChallengeType { get => Http01ChallengeValidationDetails.Http01ChallengeType; }
    }

    public abstract class ValidationPluginOptions<T> : ValidationPluginOptions where T : IValidationPlugin
    {
        public override abstract string Name { get; }
        public override abstract string Description { get; }

        public override void Show(IInputService input)
        {
            input.Show("Validation");
            input.Show("Plugin", $"{Name} - ({Description})", level: 1);
        }

        public override Type Instance => typeof(T);
    }
}
