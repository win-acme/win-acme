using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(DigitalOceanOptions))]
    internal partial class DigitalOceanJson : JsonSerializerContext
    {
        public DigitalOceanJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal class DigitalOceanOptions : ValidationPluginOptions<DigitalOcean>
    {
        public ProtectedString? ApiToken { get; set; }
    }
}