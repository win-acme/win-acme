using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("c49a7a9a-f8c9-494a-a6a4-c6b9daae7d9d")]
    internal sealed class TransIpOptions : ValidationPluginOptions<TransIp>
    {
        public override string Name { get; } = "TransIp";
        public override string Description { get; } = "Create verification records at TransIp";
        public override string ChallengeType { get; } = Constants.Dns01ChallengeType;
        public string? Login { get; internal set; }
        public ProtectedString? PrivateKey { get; internal set; }
    }
}