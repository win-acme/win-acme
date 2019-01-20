using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal abstract class HttpValidationOptions<T> : ValidationPluginOptions<T> where T : IValidationPlugin
    {
        public string Path { get; set; }
        public bool? CopyWebConfig { get; set; }
        public bool? Warmup { get; set; }

        public HttpValidationOptions() { }
        public HttpValidationOptions(HttpValidationOptions<T> source)
        {
            Path = source.Path;
            CopyWebConfig = source.CopyWebConfig;
            Warmup = source.Warmup;
        }

        public override void Show(IInputService input)
        {
            base.Show(input);
            if (!string.IsNullOrEmpty(Path))
            {
                input.Show("Path", Path, level: 1);
            }
            if (CopyWebConfig == true)
            {
                input.Show("Web.config", "Yes", level: 1);
            }
            if (Warmup == true)
            {
                input.Show("Warmup", "Yes", level: 1);
            }
        }
    }
}
