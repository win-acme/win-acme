using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal class ScriptOptions : ValidationPluginOptions
    {
        public string? Script { get; set; }
        public string? CreateScript { get; set; }
        public string? CreateScriptArguments { get; set; }
        public string? DeleteScript { get; set; }
        public string? DeleteScriptArguments { get; set; }
        public int? Parallelism { get; set; }
    }
}
