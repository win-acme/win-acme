using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services.Serialization;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    [JsonSerializable(typeof(ALiYunOptions))]
    internal partial class ALiYunJson : JsonSerializerContext
    {
        public ALiYunJson(WacsJsonPluginsOptionsFactory optionsFactory) : base(optionsFactory.Options)
        {
        }
    }

    public class ALiYunOptions : ValidationPluginOptions
    {
        /// <summary>
        /// ApiServer
        /// </summary>
        public string? ApiServer { get; set; }

        /// <summary>
        /// ApiID
        /// </summary>
        public ProtectedString? ApiID { get; set; }

        /// <summary>
        /// ApiSecret
        /// </summary>
        public ProtectedString? ApiSecret { get; set; }
    }
}
