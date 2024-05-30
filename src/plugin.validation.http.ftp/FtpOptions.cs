using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [JsonSerializable(typeof(FtpOptions))]
    public partial class FtpJson : JsonSerializerContext
    {
        public FtpJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    public class FtpOptions : HttpValidationOptions
    {
        public FtpOptions() : base() { }
        public FtpOptions(HttpValidationOptions? source) : base(source) { }

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
