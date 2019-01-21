using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("3bb22c70-358d-4251-86bd-11858363d913")]
    class ScriptOptions : InstallationPluginOptions<Script>
    {
        public override string Name => "Script";
        public override string Description => "Run a custom script";

        public string Script { get; set; }
        public string ScriptParameters { get; set; }
    }
}
