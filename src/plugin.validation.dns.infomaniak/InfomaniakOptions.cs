using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns;

[JsonSerializable(typeof(InfomaniakOptions))]
internal partial class InfomaniakJson : JsonSerializerContext
{
    public InfomaniakJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
}

internal class InfomaniakOptions : ValidationPluginOptions
{
    public ProtectedString? ApiToken { get; set; }
}
