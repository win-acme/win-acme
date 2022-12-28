using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(DigitalOceanOptions))]
    internal partial class DigitalOceanJson : JsonSerializerContext { }

    internal class DigitalOceanOptions : ValidationPluginOptions<DigitalOcean>
    {
        public override string Name => "DigitalOcean";
        public override string Description => "Create verification records on DigitalOcean";
        public override string ChallengeType => Constants.Dns01ChallengeType;
        public ProtectedString? ApiToken { get; set; }
    }
}