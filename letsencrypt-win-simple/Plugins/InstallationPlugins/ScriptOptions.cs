using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    class ScriptOptions : InstallationPluginOptions<Script>
    {
        public string Script { get; set; }
        public string ScriptParameters { get; set; }
    }
}
