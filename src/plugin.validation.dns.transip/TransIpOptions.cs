using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(TransIpOptions))]
    internal partial class TransIpJson : JsonSerializerContext
    {
        public TransIpJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options) { }
    }

    internal sealed class TransIpOptions : ValidationPluginOptions
    {
        public string? Login { get; set; }
        public ProtectedString? PrivateKey { get; set; }
    }
}