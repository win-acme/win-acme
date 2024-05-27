using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [JsonSerializable(typeof(WebDavOptions))]
    public partial class WebDavJson : JsonSerializerContext
    {
        public WebDavJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    public class WebDavOptions : HttpValidationOptions
    {
        public WebDavOptions() : base() { }
        public WebDavOptions(HttpValidationOptions? source) : base(source) { }

        /// <summary>
        /// Credentials to use for WebDav connection
        /// </summary>
        public NetworkCredentialOptions? Credential { get; set; }

        /// <summary>
        /// Show settings to user
        /// </summary>
        /// <param name="input"></param>
        public override void Show(IInputService input)
        {
            base.Show(input);
            Credential?.Show(input);
        }
    }
}
