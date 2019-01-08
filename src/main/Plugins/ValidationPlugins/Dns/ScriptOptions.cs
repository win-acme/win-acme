using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    class ScriptOptions : ValidationPluginOptions<Script>
    {
        public override string Name => "DnsScript";
        public override string Description => "Run external program/script to create and update records";
        public override string ChallengeType { get => Constants.Dns01ChallengeType; }

        public string CreateScript { get; set; }
        public string DeleteScript { get; set; }
    }
}
