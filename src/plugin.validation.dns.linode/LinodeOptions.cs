using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [JsonSerializable(typeof(LinodeOptions))]
    internal partial class LinodeJson : JsonSerializerContext
    {
        public LinodeJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class LinodeOptions : ValidationPluginOptions
    {
        public ProtectedString? ApiToken { get; set; }
    }
}