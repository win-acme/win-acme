using Newtonsoft.Json;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("3b0c3cca-db98-40b7-b678-b34791070d42")]
    internal sealed class LuaDnsOptions : ValidationPluginOptions<LuaDns>
    {
        public override string Name { get; } = "LuaDns";
        public override string Description { get; } = "Create verification records in LuaDns";
        public override string ChallengeType { get; } = Constants.Dns01ChallengeType;

        public string? Username { get; set; }
        [JsonProperty(propertyName: "APIKeySafe")]
        public ProtectedString? APIKey { get; set; }
    }
}