using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(TencentOptions))]
    internal partial class TencentJson : JsonSerializerContext
    {
        public TencentJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options)
        {
        }
    }

    public class TencentOptions : ValidationPluginOptions
    {
        /// <summary>
        /// ApiID
        /// </summary>
        public ProtectedString? ApiID { get; set; }

        /// <summary>
        /// ApiToken
        /// </summary>
        public ProtectedString? ApiKey { get; set; }
    }
}
