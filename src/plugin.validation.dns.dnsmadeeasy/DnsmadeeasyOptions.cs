using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [Plugin("13993334-2d74-4ff6-801b-833b99bf231d")]
    internal class DnsmadeeasyOptions : ValidationPluginOptions<DnsmadeeasyDnsValidation>
    {
        public override string Name => "Dnsmadeeasy";
        public override string Description => "Create verification records in Dnsmadeeasy DNS";
        public override string ChallengeType => Constants.Dns01ChallengeType;
        [JsonProperty(propertyName: "SecretSafe")]
        public ProtectedString? ApiKey { get; set; }
        public ProtectedString? ApiSecret { get; set; }
    }
}
