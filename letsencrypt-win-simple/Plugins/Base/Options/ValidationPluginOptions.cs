using ACMESharp.Authorizations;
using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;

namespace PKISharp.WACS.Plugins.Base.Options
{
    public class ValidationPluginOptions : PluginOptions
    {
        [JsonIgnore]
        public virtual string ChallengeType { get => Http01ChallengeValidationDetails.Http01ChallengeType; }
    }

    public class ValidationPluginOptions<T> : ValidationPluginOptions where T : IValidationPlugin
    {
        public override void Show(IInputService input)
        {
            input.Show("Validation", $"{Name} - ({Description})");
        }

        public override Type Instance => typeof(T);
    }
}
