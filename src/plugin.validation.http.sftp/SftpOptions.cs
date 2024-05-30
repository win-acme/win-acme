using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [JsonSerializable(typeof(SftpOptions))]
    public partial class SftpJson : JsonSerializerContext
    {
        public SftpJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    public class SftpOptions : HttpValidationOptions
    {
        public SftpOptions() : base() { }
        public SftpOptions(HttpValidationOptions? source) : base(source) { }

        /// <summary>
        /// Credentials to use for SFTP connection
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
