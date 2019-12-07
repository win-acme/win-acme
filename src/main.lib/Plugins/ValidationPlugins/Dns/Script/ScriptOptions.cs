using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [Plugin("8f1da72e-f727-49f0-8546-ef69e5ecec32")]
    internal class ScriptOptions : ValidationPluginOptions<Script>
    {
        public override string Name => "Script";
        public override string Description => "Create verification records with your own script";
        public override string ChallengeType => Constants.Dns01ChallengeType;

        public string? Script { get; set; }
        public string? CreateScript { get; set; }
        public string? CreateScriptArguments { get; set; }
        public string? DeleteScript { get; set; }
        public string? DeleteScriptArguments { get; set; }
    }
}
