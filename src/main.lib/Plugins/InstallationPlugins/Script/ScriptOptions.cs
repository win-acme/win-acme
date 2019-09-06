using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    [Plugin("3bb22c70-358d-4251-86bd-11858363d913")]
    class ScriptOptions : InstallationPluginOptions<Script>
    {
        public override string Name => "Script";
        public override string Description => "Start external script or program";

        public string Script { get; set; }
        public string ScriptParameters { get; set; }

        /// <summary>
        /// Show details to the user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            input.Show("Script", Script, level: 2);
            if (!string.IsNullOrEmpty(ScriptParameters))
            {
                input.Show("ScriptParameters", ScriptParameters, level: 2);
            }
        }
    }
}
