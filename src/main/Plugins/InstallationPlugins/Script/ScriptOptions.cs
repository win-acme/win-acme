using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class ScriptOptions : InstallationPluginOptions<Script>
    {
        public override string Name => "Script";
        public override string Description => "Run a custom script";

        public string Script { get; set; }
        public string ScriptParameters { get; set; }
    }
}
