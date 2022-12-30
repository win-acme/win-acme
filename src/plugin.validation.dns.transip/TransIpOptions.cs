using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(TransIpOptions))]
    internal partial class TransIpJson : JsonSerializerContext
    {
        public TransIpJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal sealed class TransIpOptions : ValidationPluginOptions<TransIp>
    {
        public override string Name { get; } = "TransIp";
        public override string Description { get; } = "Create verification records at TransIp";
        public override string ChallengeType { get; } = Constants.Dns01ChallengeType;
        public string? Login { get; set; }
        public ProtectedString? PrivateKey { get; set; }
    }
}